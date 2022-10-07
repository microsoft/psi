using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMU.Smartlab.Communication
{
    public class ImageSendHelper
    {
        public volatile CommunicationManager manager;
        public string Name;
        public int SendingWidth;
        public string SendingTopic;
        public object Lock;
        private DateTime frameTime = new DateTime(0);

        public string SizeTopic
        {
            get
            {
                return $"{SendingTopic}_Size";
            }
        }

        public string PropertyTopic
        {
            get
            {
                return $"{SendingTopic}_Prop";
            }
        }

        public string ImageTopic
        {
            get
            {
                return $"{SendingTopic}_Image";
            }
        }

        public ImageSendHelper(CommunicationManager manager, string name, string topic, int width, object sendingLock)
        {
            this.Name = name;
            this.SendingTopic = topic;
            this.SendingWidth = width;
            this.Lock = sendingLock;
            this.manager = manager;
        }

        public void SendImage(Shared<Image> image, Envelope envelope)
        {
            Image rawData = image.Resource;
            Task task = new Task(() =>
            {
                lock (this.Lock)
                {
                    try
                    {
                        // Console.WriteLine($"Width = {rawData.Width}, Height = {rawData.Height}, Format = {rawData.PixelFormat}");
                        int w = rawData.Width;
                        float scale = (float)SendingWidth / w;
                        var sharedScaledImage = rawData.Scale(scale, scale, SamplingMode.Bilinear);
                        rawData = sharedScaledImage.Resource;
                        // Console.WriteLine($"After scaling: Width = {rawData.Width}, Height = {rawData.Height}, Format = {rawData.PixelFormat}");
                        this.manager.SendText(this.SizeTopic, $"{rawData.Width}:{rawData.Height}");
                        this.manager.SendText(this.PropertyTopic, $"camera_id:str:{this.Name}");
                        this.manager.SendText(this.PropertyTopic, $"timestamp:int:{this.frameTime.Ticks}");
                        this.manager.SendText(this.PropertyTopic, $"END");
                        this.manager.SendImage(this.ImageTopic, rawData);
                        sharedScaledImage.Dispose();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    finally
                    {
                        this.manager.Occupied = false;
                    }
                }
            });
            if (!this.manager.Occupied && envelope.OriginatingTime.CompareTo(this.frameTime) > 0)
            {
                this.manager.Occupied = true;
                frameTime = envelope.OriginatingTime;
                task.Start();
            }
        }
    }
}
