using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Timers;
using NetMQ; // for NetMQConfig
using NetMQ.Sockets;
using MsgPack;

namespace Pupil
{
// Sample:
//{ 
//    "diameter" : 0, 
//    "confidence" : 0, 
//    "projected_sphere" : { 
//        "axes" : [ 55.5000089570384, 55.5000089570384 ], 
//        "angle" : 90, 
//        "center" : [ 249.397085868656, 136.095677317297 ] 
//    }, 
//    "theta" : 0, 
//    "model_id" : 1, 
//    "timestamp" : 16348.113020359, 
//    "model_confidence" : 1, 
//    "method" : "3d c++", 
//    "phi" : 0, 
//    "sphere" : { 
//        "radius" : 12, 
//        "center" : [ -30.5309849672982, -44.9315917465023, 268.108064838663 ] 
//    }, 
//    "diameter_3d" : 0, 
//    "norm_pos" : [ 0.5, 0.5 ], 
//    "id" : 0, 
//    "model_birth_timestamp" : 11561.005596197, 
//    "circle_3d" : { 
//        "radius" : 0, 
//        "center" : [ 0, 0, 0 ], 
//        "normal" : [ 0, 0, 0 ] 
//    }, 
//    "ellipse" : { 
//        "axes" : [ 0, 0 ], 
//        "angle" : 90, 
//        "center" : [ 320, 240 ] 
//    } 
//}

    // Pupil data typea
    [Serializable]
    public class ProjectedSphere
    {
        public double[] axes = new double[] {0,0};
        public double angle;
        public double[] center = new double[] {0,0};
    }
    [Serializable]
    public class Sphere
    {
        public double radius;
        public double[] center = new double[] {0,0,0};
    }
    [Serializable]
    public class Circle3d
    {
        public double radius;
        public double[] center = new double[] {0,0,0};
        public double[] normal = new double[] {0,0,0};
    }
    [Serializable]
    public class Ellipse
    {
        public double[] axes = new double[] {0,0};
        public double angle;
        public double[] center = new double[] {0,0};
    }
    [Serializable]
    public class PupilData3D
    {
        public double diameter;
        public double confidence;
        public ProjectedSphere projected_sphere = new ProjectedSphere();
        public double theta;
        public int model_id;
        public double timestamp;
        public double model_confidence;
        public string method;
        public double phi;
        public Sphere sphere = new Sphere();
        public double diameter_3d;
        public double[] norm_pos = new double[] { 0, 0, 0 };
        public int id;
        public double model_birth_timestamp;
        public Circle3d circle_3d = new Circle3d();
        public Ellipse ellipese = new Ellipse();
    }
}

public class PupilListener : MonoBehaviour
{

    Thread client_thread_;
    private System.Object thisLock_ = new System.Object();
    bool stop_thread_ = false;
    public string IP = "192.168.11.36";// IP of a PC running pupil_capture/_remote
    public string PORT = "50020"; // port of the PC
    public string ID = "pupil.0"; // target camera


    Pupil.PupilData3D data_ = new  Pupil.PupilData3D();

    public void get_transform(ref Vector3 pos, ref Quaternion q)
    {
        lock (thisLock_)
        {
            pos = new Vector3(
                        (float)(data_.sphere.center[0]),
                        (float)(data_.sphere.center[1]),
                        (float)(data_.sphere.center[2])
                        )*0.001f;// in [m]
            q = Quaternion.LookRotation(new Vector3(
            (float)(data_.circle_3d.normal[0]),
            (float)(data_.circle_3d.normal[1]),
            (float)(data_.circle_3d.normal[2])
            ));
        }
    }

    void Start()
    {
        Debug.Log("Start a request thread.");
        client_thread_ = new Thread(NetMQClient);
        client_thread_.Start();
    }

    // Client thread which does not block Update()
    void NetMQClient()
    {
        string IPHeader = ">tcp://" + IP + ":";
        var timeout = new System.TimeSpan(0, 0, 1); //1sec

        // Necessary to handle this NetMQ issue on Unity editor
        // https://github.com/zeromq/netmq/issues/526
        AsyncIO.ForceDotNet.Force();
        NetMQConfig.ManualTerminationTakeOver();
        NetMQConfig.ContextCreate(true);
        
        string subport="";
        Debug.Log("Connect to the server: "+ IPHeader + PORT + ".");
        var requestSocket = new RequestSocket(IPHeader + PORT);
        double t = 0;
        const int N = 1000;
        bool is_connected =false;
        for (int k = 0; k < N; k++)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            requestSocket.SendFrame("SUB_PORT");
            is_connected = requestSocket.TryReceiveFrameString(timeout, out subport);
            sw.Stop();
            t = t+ sw.Elapsed.Milliseconds;
            //Debug.Log("Round trip time:" + sw.Elapsed + "[sec].");
            if (is_connected == false) break;
        }
        Debug.Log("Round trip average time:" + t/N + "[msec].");

        requestSocket.Close();

        if (is_connected)
        {
            // 
            var subscriberSocket = new SubscriberSocket( IPHeader + subport);
            subscriberSocket.Subscribe(ID);

            var msg = new NetMQMessage();
            while (is_connected && stop_thread_ == false)
            {
                Debug.Log("Receive a multipart message.");
                is_connected = subscriberSocket.TryReceiveMultipartMessage(timeout,ref(msg));
                if (is_connected)
                {
                    Debug.Log("Unpack a received multipart message.");
                    try
                    {
                        //Debug.Log(msg[0].ConvertToString());
                        var message = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
                        MsgPack.MessagePackObject mmap = message.Value;
                        lock (thisLock_)
                        {
                            data_ = JsonUtility.FromJson<Pupil.PupilData3D>(mmap.ToString());
                        }
                        //Debug.Log(message);
                    }
                    catch
                    {
                        Debug.Log("Failed to unpack.");
                    }
                }
                else
                {
                    Debug.Log("Failed to receive a message.");
                    Thread.Sleep(1000);
                }
            }
            subscriberSocket.Close();
        }
        else
        {
            Debug.Log("Failed to connect the server.");
        }

        // Necessary to handle this NetMQ issue on Unity editor
        // https://github.com/zeromq/netmq/issues/526
        Debug.Log("ContextTerminate.");
        NetMQConfig.ContextTerminate();
    }

    void Update()
    {
        /// Do normal Unity stuff
    }

    void OnApplicationQuit()
    {
        lock (thisLock_)stop_thread_ = true;
        client_thread_.Join();
        Debug.Log("Quit the thread.");
    }

}