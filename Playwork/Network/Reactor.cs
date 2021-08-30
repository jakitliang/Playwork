using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Playwork.Network {
    public interface IEventHandler
    {
        int OnRead(IO io);
        int OnWrite(IO io);
        void OnAttach(IO io);
        void OnDetach(IO io);
    }

    public class Reactor
    {
        public const int FLAG_READ = 1;
        public const int FLAG_WRITE = 2;

        public const int HANDLE_FAILED = -1;
        public const int HANDLE_CONTINUED = 0;
        public const int HANDLE_FINISHED = 1;

        private class Event
        {
            public const int OP_ADD = 0;
            public const int OP_MOD = 1;
            public const int OP_DEL = 2;

            public IO IO { get; set; }

            public IEventHandler Handler { get; set; }

            public int Flag { get; set; }

            public int Operation { get; set; }
        }

        private static Reactor instance;

        private Thread thread;
        private Mutex pushLock;
        private Queue<Event> taskQueue;
        private Dictionary<Socket, IO> sockets;
        private Dictionary<IO, int> ios;
        private Dictionary<IO, IEventHandler> handlers;
        private UDPServer pipeIn;
        private UDPSocket pipeOut;

        private Reactor()
        {
            Console.WriteLine("Event init");
            taskQueue = new Queue<Event>();
            pushLock = new Mutex();
            sockets = new Dictionary<Socket, IO>();
            ios = new Dictionary<IO, int>();
            handlers = new Dictionary<IO, IEventHandler>();

            pipeIn = new UDPServer("127.0.0.1", 0);
            pipeOut = new UDPSocket("127.0.0.1", ((IPEndPoint) pipeIn.Socket.LocalEndPoint).Port);
            sockets.Add(pipeIn.Socket, pipeIn);
            ios.Add(pipeIn, FLAG_READ);

            thread = new Thread(Loop);
            thread.Start();
        }

        static Reactor()
        {
            Console.WriteLine("Event create");
            instance = new Reactor();
        }

        private List<Socket> GetPollReadList()
        {
            var found = ios.Where(p => (p.Value & 1) > 0).Select(p => p.Key.Socket).ToList();

            return found;
        }

        private List<Socket> GetPollWriteList()
        {
            var found = ios.Where(p => (p.Value & 2) > 0).Select(p => p.Key.Socket).ToList();

            return found;
        }

        private void ProcessQueue()
        {
            Event ev;
            Console.WriteLine("Event process queue");

            pushLock.WaitOne();

            byte[] symByte = new byte[1];
            pipeIn.Read(symByte, 1);
            char[] symName = System.Text.Encoding.UTF8.GetChars(symByte);

            if (symName[0] != 'a') {
                return;
            }

            ev = taskQueue.Dequeue();

            switch (ev.Operation)
            {
                case Event.OP_ADD:
                    Console.WriteLine("Event process queue add task");
                    sockets.Add(ev.IO.Socket, ev.IO);
                    ios.Add(ev.IO, ev.Flag);

                    if (ev.Handler != null) {
                        handlers.Add(ev.IO, ev.Handler);
                        ev.Handler.OnAttach(ev.IO);
                    }

                    break;

                case Event.OP_MOD:
                    Console.WriteLine("Event process queue mod task");
                    if (ios.ContainsKey(ev.IO))
                    {
                        ios[ev.IO] = ev.Flag;
                    }

                    break;

                case Event.OP_DEL:
                    Console.WriteLine("Event process queue del task");
                    if (ios.ContainsKey(ev.IO))
                    {
                        sockets.Remove(ev.IO.Socket);
                        ios.Remove(ev.IO);
                    }

                    if (handlers.ContainsKey(ev.IO))
                    {
                        IEventHandler handler = handlers[ev.IO];
                        handler.OnDetach(ev.IO);
                        handlers.Remove(ev.IO);
                    }

                    break;
            }

            pushLock.ReleaseMutex();
        }

        private void ProcessReadable(List<Socket> readable)
        {
            Console.WriteLine("Event process readable");

            foreach (Socket socket in readable)
            {
                if (!sockets.ContainsKey(socket))
                {
                    continue;
                }

                if (socket == pipeIn.Socket) {
                    ProcessQueue();
                    continue;
                }

                IO io = sockets[socket];

                IEventHandler handler = handlers[io];
                int result = handler.OnRead(io);

                switch (result)
                {
                    case HANDLE_FINISHED:
                        ios[io] &= ~FLAG_READ;
                        break;

                    case HANDLE_FAILED:
                        handler.OnDetach(io);

                        ios.Remove(io);
                        sockets.Remove(io.Socket);
                        handlers.Remove(io);
                        break;
                }
            }
        }

        private void ProcessWritable(List<Socket> writable)
        {
            Console.WriteLine("Event process writable");

            foreach (Socket socket in writable)
            {
                if (!sockets.ContainsKey(socket))
                {
                    continue;
                }

                IO io = sockets[socket];

                IEventHandler handler = handlers[io];
                int result = handler.OnWrite(io);

                switch (result)
                {
                    case HANDLE_FINISHED:
                        ios[io] &= ~FLAG_WRITE;
                        break;

                    case HANDLE_FAILED:
                        handler.OnDetach(io);

                        ios.Remove(io);
                        sockets.Remove(io.Socket);
                        handlers.Remove(io);
                        break;
                }
            }
        }

        private void Loop()
        {
            Console.WriteLine("Event loop start");

            while (true)
            {
                List<Socket> pollRead = GetPollReadList();
                List<Socket> pollWrite = GetPollWriteList();

                Console.WriteLine("Event polling...");

                try
                {
                    Socket.Select(pollRead, pollWrite, null, System.Int32.MaxValue);
                    //Socket.Select(pollRead, pollWrite, null, 5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Thread.Sleep(2000);
                }

                ProcessReadable(pollRead);
                ProcessWritable(pollWrite);
            }
        }

        private void AddTask(Event task) {
            pushLock.WaitOne();

            taskQueue.Enqueue(task);
            pipeOut.Write(System.Text.Encoding.UTF8.GetBytes("a"), 1);

            pushLock.ReleaseMutex();
        }

        public static void Attach(IO io, IEventHandler handler, int flag)
        {
            Event ev = new Event();
            ev.IO = io;
            ev.Handler = handler;
            ev.Flag = flag;
            ev.Operation = Event.OP_ADD;

            instance.AddTask(ev);

            Console.WriteLine("Event attach");
        }

        public static void Modify(IO io, IEventHandler handler, int flag)
        {
            Event ev = new Event();
            ev.IO = io;
            ev.Handler = handler;
            ev.Flag = flag;
            ev.Operation = Event.OP_MOD;

            instance.AddTask(ev);

            Console.WriteLine("Event modify");
        }

        public static void Detach(IO io)
        {
            Event ev = new Event();
            ev.IO = io;
            ev.Handler = null;
            ev.Flag = 0;
            ev.Operation = Event.OP_DEL;

            instance.AddTask(ev);

            Console.WriteLine("Event detach");
        }
    }
}
