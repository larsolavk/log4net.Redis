using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using log4net.Core;

namespace log4net.Redis.Layout
{
    public class JsonEventLayout : log4net.Layout.LayoutSkeleton
    {
        private string UserFields { get; set; }
        private bool AddSequence { get; set; }

        private int _sequence;

        public override void ActivateOptions()
        {
        }

        public override string ContentType
        {
            get
            {
                return "application/json";
            }
        }

        public override void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            using (var jw = new JsonTextWriter(writer))
            {
                jw.WriteStartObject();
                jw.WritePropertyName("@version");
                jw.WriteValue(1);
                jw.WritePropertyName("@timestamp");
                jw.WriteValue(loggingEvent.TimeStamp.ToUniversalTime().ToString("o"));

                jw.WritePropertyName("source_host");
                jw.WriteValue(System.Environment.MachineName);

                jw.WritePropertyName("message");
                jw.WriteValue(loggingEvent.RenderedMessage);

                jw.WritePropertyName("logger_name");
                jw.WriteValue(loggingEvent.LoggerName);

                jw.WritePropertyName("level");
                jw.WriteValue(loggingEvent.Level.ToString());

                jw.WritePropertyName("thread_name");
                jw.WriteValue(loggingEvent.ThreadName);

                if (!String.IsNullOrWhiteSpace(UserFields))
                {
                    var fields = UserFields.Split(',');
                    foreach (var field in fields)
                    {
                        var keyVal = field.Split(':');
                        if (keyVal.Length == 2)
                        {
                            jw.WritePropertyName(keyVal[0].Trim());
                            jw.WriteValue(keyVal[1].Trim());
                        }
                    }
                }

                if (AddSequence)
                {
                    System.Threading.Interlocked.CompareExchange(ref _sequence, 0, int.MaxValue);

                    jw.WritePropertyName("sequence");
                    jw.WriteValue(System.Threading.Interlocked.Increment(ref _sequence));
                }


                jw.WriteEndObject();
            }
            
        }
    }
}
