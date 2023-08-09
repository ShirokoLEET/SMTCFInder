using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog;
using System;
using Windows.Media.Control;
using WindowsMediaController;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.IO;
using System.Windows.Media.Imaging;
using Windows.UI.Xaml.Controls;

namespace Sample.CMD
{
    class Program
    {
        
        private static string Title;
        private static string Artist;
        private static TimeSpan Postion;
        private static TimeSpan Endtime;
        static MediaManager mediaManager;
        static readonly object _writeLock = new object();
        static MediaManager.MediaSession Session;
        static bool ispaused;
        static byte[] imageByt3s;
        
        public static async Task Main()
        {
            mediaManager = new MediaManager()
            {
                Logger = BuildLogger("MediaManager"),
            };
            mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            WriteLineColor("Unable to find your music player?maybe it doesnt support windows SMTC.Find a solution or change your player <3 ", ConsoleColor.Gray);
            mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;
           
            mediaManager.Start();
            pausecheck(); 
           await getThumbnail();
            WriteLineColor("Run as admintractor so get acess to C:/1.jpg", ConsoleColor.Red);
            await httpstart();
             Console.ReadLine();
            mediaManager.Dispose();
                 }
        private static async Task getThumbnail()
        {
            
            {
                GlobalSystemMediaTransportControlsSessionMediaProperties songInfo =  Session.ControlSession.TryGetMediaPropertiesAsync().AsTask().GetAwaiter().GetResult();
                if (songInfo != null)
                {
                    System.Windows.Media.Imaging.BitmapImage songThumbnail = Helper.GetThumbnail(songInfo.Thumbnail);
                    if (songThumbnail != null)
                    {
                        byte[] imageBytes;
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder(); // 使用 JPEG 编码器，你可以根据需要选择其他编码器
                        encoder.Frames.Add(BitmapFrame.Create(songThumbnail));
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            encoder.Save(memoryStream);
                            imageBytes = memoryStream.ToArray();
                        }
                        
                        string filepath = "C:\\1.jpg";
                        File.Delete(filepath);
                        if (!File.Exists(filepath)){
                           
                            using (FileStream fileStream = File.Create(filepath)) ;
                            

                        }
                       
                        File.WriteAllBytes(filepath, imageBytes);
                       

                    }
                }
            }
            
        }
        internal static class Helper
        {
            internal static System.Windows.Media.Imaging.BitmapImage GetThumbnail(IRandomAccessStreamReference thumbnail)
            {
                if (thumbnail == null)
                    return null;

                using (IRandomAccessStreamWithContentType thumbnailStream = thumbnail.OpenReadAsync().AsTask().GetAwaiter().GetResult())
                {
                    System.Windows.Media.Imaging.BitmapImage image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = thumbnailStream.AsStream();
                    image.EndInit();
                    return image;

                }
            }
        }
        private static void pausecheck()
        {
            var mediaProp = Session.ControlSession.GetPlaybackInfo();
            if (mediaProp != null)
            {
                if (Session.ControlSession.GetPlaybackInfo().Controls.IsPauseEnabled)
                    ispaused = false;
                else
                    ispaused = true;
            }
        }

        public static async Task httpstart()
        {
            int port = 8816;  // 选择一个空闲的端口号

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            Console.WriteLine($"Listening on port {port}...");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                 ProcessRequestAsync(context);
            }
        }

        static async Task ProcessRequestAsync(HttpListenerContext context)
        {
                
                string responmessage = null;

                string requestContent;
                using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                {
                    requestContent = await reader.ReadToEndAsync();
                    Console.WriteLine($"Received request content: {requestContent}");


                }


                // 根据接收到的文本进行处理
                if (requestContent == "mediaupdate")
                {   
                    pausecheck();
                    getThumbnail();
                responmessage = Title + "--Artist:" + Artist + "--Postion:" + Postion + "--Endtime:" + Endtime+"--isPaused:"+ispaused ;

                }
                else if (requestContent == "mediaskip")
                {
                    await mediaskip(Session);
                }
                else if (requestContent == "mediapause")
                {
                    await mediapause(Session);
                } else if (requestContent == "mediacontiune")
                {
                    await mediacontiune(Session);
                } else if (requestContent == "mediabefore")
                {
                    await mediabefore(Session);
                }
                else 
            {

                responmessage = "owo";

            }
           
                byte[] buffer = Encoding.UTF8.GetBytes(responmessage);
                context.Response.ContentType = "text/plain";

                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);

                context.Response.OutputStream.Close();
                context.Response.Close();




            }
            


        public async static Task mediabefore(MediaManager.MediaSession mediaSession)
        {
            var controlsession = mediaSession.ControlSession;
            await mediaSession.ControlSession.TrySkipPreviousAsync();

        }
        public async static Task mediaskip(MediaManager.MediaSession mediaSession)
        {
            var controlsession = mediaSession.ControlSession;
            await mediaSession.ControlSession.TrySkipNextAsync();

        }
        public async static Task mediacontiune(MediaManager.MediaSession mediaSession)
        {
            var controlsession = mediaSession.ControlSession;
            await mediaSession.ControlSession.TryPlayAsync();

        }
        public async static Task mediapause(MediaManager.MediaSession mediaSession)
        {
            var controlsession = mediaSession.ControlSession;
            await mediaSession.ControlSession.TryPauseAsync();
        }


        private static void MediaManager_OnAnySessionOpened(MediaManager.MediaSession session)
        {
            WriteLineColor("-- New Source: " + session.Id, ConsoleColor.Green);
        }
        private static void MediaManager_OnAnySessionClosed(MediaManager.MediaSession session)
        {
            WriteLineColor("-- Removed Source: " + session.Id, ConsoleColor.Red);
        }

        private static void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession mediaSession)
        {
            WriteLineColor("== Session Focus Changed: " + mediaSession?.ControlSession?.SourceAppUserModelId, ConsoleColor.Gray);
            Session = mediaSession;
        }

        private static void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
        {  
            WriteLineColor($"{sender.Id} is now {args.PlaybackStatus}", ConsoleColor.Yellow);
            pausecheck();
            Console.WriteLine(ispaused);
        }

        private static void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
        {
            
            WriteLineColor($"{sender.Id} is now playing {args.Title} {(string.IsNullOrEmpty(args.Artist) ? "" : $"by {args.Artist}")}", ConsoleColor.Cyan);
            Title = args.Title;
            Artist = args.Artist;
             getThumbnail();
        }

        private static void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionTimelineProperties args)
        {
            WriteLineColor($"{sender.Id} timeline is now {args.Position}/{args.EndTime}", ConsoleColor.Magenta);
            Postion = args.Position;
            Endtime = args.EndTime;
        }

        public static void WriteLineColor(object toprint, ConsoleColor color = ConsoleColor.White)
        {
            lock (_writeLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + toprint);
            }
        }

        private static Microsoft.Extensions.Logging.ILogger BuildLogger(string sourceContext = null)
        {
            return new LoggerFactory().AddSerilog(logger: new LoggerConfiguration()
                    .MinimumLevel.Is(LogEventLevel.Information)
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] " + (sourceContext ?? "{SourceContext}") + ": {Message:lj}{NewLine}{Exception}")
                    .CreateLogger())
                    .CreateLogger(string.Empty);
                    
        }
    }
}
