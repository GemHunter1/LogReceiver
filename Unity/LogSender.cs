using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class LogSender : MonoBehaviour
{
    public string ipAddress;
    public int port;

    public LogLevel sendStacktrace = LogLevel.Everything;

    [Flags]
    public enum LogLevel
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 4,
        Everything = Info | Warning | Error
    }

    [Space]
    public bool sendInEditor = false;

    private TcpClient tcpClient;
    private Thread senderThread;

    private BlockingCollection<string> logQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());

    private bool isRunning = true;
    private CancellationTokenSource cts;

    void Awake()
    {
        if (!sendInEditor && Application.isEditor)
            return;

        cts = new CancellationTokenSource();
        Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
        senderThread = new Thread(Work);
        senderThread.Start();
    }

    void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= Application_logMessageReceivedThreaded;
        isRunning = false;
        cts?.Cancel();
    }

    private void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        if (isRunning)
        {
            if (sendStacktrace != LogLevel.None && 
                (sendStacktrace == LogLevel.Everything 
                || (type == LogType.Warning && sendStacktrace.HasFlag(LogLevel.Warning)) 
                || ((type == LogType.Error || type == LogType.Exception) && sendStacktrace.HasFlag(LogLevel.Error))
                ))
            {
                logQueue.Add($"[{DateTime.Now:g}][{type}] {condition}\n{stackTrace}");
            }
            else
            {
                logQueue.Add($"[{DateTime.Now:g}][{type}] {condition}");
            }
        }
    }

    private void Work()
    {
        NetworkStream ns = null;
        StreamWriter writer = null;

        try
        {
            tcpClient = new TcpClient(ipAddress, port);

            ns = tcpClient.GetStream();
            writer = new StreamWriter(ns);

            while (isRunning)
            {
                string logEntry = logQueue.Take(cts.Token);
                writer.WriteLine(logEntry);
                writer.Flush();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            isRunning = false;
            Application.logMessageReceivedThreaded -= Application_logMessageReceivedThreaded;
            Debug.LogError("[LogSender] " + ex.ToString());
        }
        finally
        {
            ns?.Dispose();
            tcpClient?.Close();
        }
    }
}
