using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Devices.Geolocation;
using Apricot;

namespace Weather
{
    [System.Composition.Export(typeof(IExtension))]
    public class WeatherExtension : IExtension
    {
        private readonly string apiKey = "YOUR_API_KEY";
        private Geolocator geolocator = null;
        private DispatcherTimer timer = null;

        public WeatherExtension()
        {
            this.geolocator = new Geolocator();
            this.geolocator.MovementThreshold = 1000;
            this.geolocator.PositionChanged += this.PositionChanged;
            this.timer = new DispatcherTimer(DispatcherPriority.Background);
            this.timer.Interval = TimeSpan.FromMinutes(15);
            this.timer.Tick += new EventHandler(async (sender, args) =>
            {
                if (this.geolocator.LocationStatus == PositionStatus.Ready && this.timer.IsEnabled)
                {
                    Geoposition geoposition;

                    try
                    {
                        geoposition = await this.geolocator.GetGeopositionAsync();
                    }
                    catch
                    {
                        geoposition = null;
                    }

                    if (geoposition != null && geoposition.Coordinate.Accuracy <= 1000)
                    {
                        Update(geoposition.Coordinate);
                    }
                }
            });
        }

        public async void Attach()
        {
            this.timer.Start();
            
            if (this.geolocator.LocationStatus == PositionStatus.Ready)
            {
                Geoposition geoposition;

                try
                {
                    geoposition = await this.geolocator.GetGeopositionAsync();
                }
                catch
                {
                    geoposition = null;
                }

                if (geoposition != null && geoposition.Coordinate.Accuracy <= 1000)
                {
                    Update(geoposition.Coordinate);
                }
            }
        }

        public void Detach()
        {
            this.timer.Stop();
        }

        private async void Update(Geocoordinate geocoordinate)
        {
            Configuration config1 = null;
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly().GetName().Name);
            var webRequest = WebRequest.Create(new Uri(String.Format("https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&APPID={2}", Utility.UrlEncode(geocoordinate.Point.Position.Latitude.ToString(CultureInfo.InvariantCulture)), Utility.UrlEncode(geocoordinate.Point.Position.Longitude.ToString(CultureInfo.InvariantCulture)), this.apiKey)));
            Queue<string> queue;

            if (Directory.Exists(directory))
            {
                var filename = Path.GetFileName(Assembly.GetEntryAssembly().Location);

                foreach (var s in Directory.EnumerateFiles(directory, "*.config", SearchOption.TopDirectoryOnly))
                {
                    if (filename.Equals(Path.GetFileNameWithoutExtension(s)))
                    {
                        var exeConfigurationFileMap = new ExeConfigurationFileMap();

                        exeConfigurationFileMap.ExeConfigFilename = s;
                        config1 = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None);
                    }
                }
            }

            if (config1 == null)
            {
                config1 = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config1.AppSettings.Settings["Timeout"] != null && config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                {
                    webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                }

                if (config1.AppSettings.Settings["UserAgent"] != null)
                {
                    var httpWebRequest = webRequest as HttpWebRequest;

                    if (httpWebRequest != null)
                    {
                        httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                    }
                }
            }
            else
            {
                var config2 = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config1.AppSettings.Settings["Timeout"] == null)
                {
                    if (config2.AppSettings.Settings["Timeout"] != null && config2.AppSettings.Settings["Timeout"].Value.Length > 0)
                    {
                        webRequest.Timeout = Int32.Parse(config2.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                    }
                }
                else if (config1.AppSettings.Settings["Timeout"].Value.Length > 0)
                {
                    webRequest.Timeout = Int32.Parse(config1.AppSettings.Settings["Timeout"].Value, CultureInfo.InvariantCulture);
                }

                if (config1.AppSettings.Settings["UserAgent"] == null)
                {
                    if (config2.AppSettings.Settings["UserAgent"] != null)
                    {
                        var httpWebRequest = webRequest as HttpWebRequest;

                        if (httpWebRequest != null)
                        {
                            httpWebRequest.UserAgent = config2.AppSettings.Settings["UserAgent"].Value;
                        }
                    }
                }
                else
                {
                    var httpWebRequest = webRequest as HttpWebRequest;

                    if (httpWebRequest != null)
                    {
                        httpWebRequest.UserAgent = config1.AppSettings.Settings["UserAgent"].Value;
                    }
                }
            }

            try
            {
                queue = await Task.Factory.StartNew<Queue<string>>(delegate (object state)
                {
                    Queue<string> q = null;

                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        WebRequest request = (WebRequest)state;
                        WebResponse response = null;
                        Stream s = null;
                        StreamReader sr = null;

                        try
                        {
                            response = request.GetResponse();
                            s = response.GetResponseStream();
                            sr = new StreamReader(s);
                            s = null;

                            var jsonDictionary = Json.JsonDecode(sr.ReadToEnd()) as Dictionary<string, object>;

                            if (jsonDictionary != null)
                            {
                                object weather;

                                if (jsonDictionary.TryGetValue("weather", out weather))
                                {
                                    var weathers = weather as object[];

                                    if (weathers != null)
                                    {
                                        var nowDateTime = DateTime.Now;

                                        q = new Queue<string>();

                                        foreach (object o1 in weathers)
                                        {
                                            var dictionary = o1 as Dictionary<string, object>;

                                            if (dictionary != null)
                                            {
                                                object o2;

                                                if (dictionary.TryGetValue("id", out o2))
                                                {
                                                    if (o2 is double)
                                                    {
                                                        var digit = Convert.ToInt32(o2);

                                                        if (digit == 701)
                                                        {
                                                            q.Enqueue("Mist");
                                                        }
                                                        else if (digit == 711)
                                                        {
                                                            q.Enqueue("Smoke");
                                                        }
                                                        else if (digit == 721)
                                                        {
                                                            q.Enqueue("Haze");
                                                        }
                                                        else if (digit == 731)
                                                        {
                                                            q.Enqueue("Dust");
                                                        }
                                                        else if (digit == 741)
                                                        {
                                                            q.Enqueue("Fog");
                                                        }
                                                        else if (digit == 800)
                                                        {
                                                            if (nowDateTime.Hour > 6 && nowDateTime.Hour <= 18)
                                                            {
                                                                q.Enqueue("Sunny");
                                                            }
                                                            else
                                                            {
                                                                q.Enqueue("Clear");
                                                            }
                                                        }
                                                        else if (digit >= 801 && digit <= 803)
                                                        {
                                                            q.Enqueue("Cloudy");
                                                        }
                                                        else if (digit == 804)
                                                        {
                                                            q.Enqueue("Overcast");
                                                        }
                                                        else if (digit == 900)
                                                        {
                                                            q.Enqueue("Tornado");
                                                        }
                                                        else if (digit == 905)
                                                        {
                                                            q.Enqueue("Windy");
                                                        }
                                                        else if (digit == 906)
                                                        {
                                                            q.Enqueue("Hail");
                                                        }
                                                        else
                                                        {
                                                            var i = digit / 100;

                                                            if (i == 2)
                                                            {
                                                                q.Enqueue("Thunderstorm");
                                                            }
                                                            else if (i == 3)
                                                            {
                                                                q.Enqueue("Drizzle");
                                                            }
                                                            else if (i == 5)
                                                            {
                                                                q.Enqueue("Rain");
                                                            }
                                                            else if (i == 6)
                                                            {
                                                                q.Enqueue("Snow");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (sr != null)
                            {
                                sr.Close();
                            }

                            if (s != null)
                            {
                                s.Close();
                            }

                            if (response != null)
                            {
                                response.Close();
                            }
                        }
                    }

                    return q;
                }, webRequest, TaskCreationOptions.LongRunning);
            }
            catch
            {
                queue = null;
            }
            
            if (queue != null)
            {
                var sequenceList = new List<Sequence>();

                foreach (var sequence in Script.Instance.Sequences)
                {
                    if (sequence.Name.Equals("Weather"))
                    {
                        sequenceList.Add(sequence);
                    }
                }

                while (queue.Count > 0)
                {
                    Script.Instance.TryEnqueue(Script.Instance.Prepare(sequenceList, queue.Dequeue()));
                }
            }
        }

        private void PositionChanged(Geolocator geolocator, PositionChangedEventArgs positionChangedEventArgs)
        {
            if (positionChangedEventArgs.Position.Coordinate.Accuracy <= 1000)
            {
                Update(positionChangedEventArgs.Position.Coordinate);
            }
        }
    }

    internal class Json
    {
        public const int TOKEN_NONE = 0;
        public const int TOKEN_CURLY_OPEN = 1;
        public const int TOKEN_CURLY_CLOSE = 2;
        public const int TOKEN_SQUARED_OPEN = 3;
        public const int TOKEN_SQUARED_CLOSE = 4;
        public const int TOKEN_COLON = 5;
        public const int TOKEN_COMMA = 6;
        public const int TOKEN_STRING = 7;
        public const int TOKEN_NUMBER = 8;
        public const int TOKEN_TRUE = 9;
        public const int TOKEN_FALSE = 10;
        public const int TOKEN_NULL = 11;

        /// <summary>
        /// Parses the string json into a value
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
        public static object JsonDecode(string json)
        {
            bool success = true;

            return JsonDecode(json, ref success);
        }

        /// <summary>
        /// Parses the string json into a value; and fills 'success' with the successfullness of the parse.
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <param name="success">Successful parse?</param>
        /// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
        public static object JsonDecode(string json, ref bool success)
        {
            success = true;

            if (json != null)
            {
                char[] charArray = json.ToCharArray();
                int index = 0;
                object value = ParseValue(charArray, ref index, ref success);
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a Hashtable / ArrayList object into a JSON string
        /// </summary>
        /// <param name="json">A Hashtable / ArrayList</param>
        /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
        public static string JsonEncode(object json)
        {
            StringBuilder builder = new StringBuilder();
            bool success = SerializeValue(json, builder);

            return (success ? builder.ToString() : null);
        }

        protected static Dictionary<string, object> ParseObject(char[] json, ref int index, ref bool success)
        {
            Dictionary<string, object> table = new Dictionary<string, object>();
            int token;

            // {
            NextToken(json, ref index);

            bool done = false;

            while (!done)
            {
                token = LookAhead(json, index);

                if (token == Json.TOKEN_NONE)
                {
                    success = false;

                    return null;
                }
                else if (token == Json.TOKEN_COMMA)
                {
                    NextToken(json, ref index);
                }
                else if (token == Json.TOKEN_CURLY_CLOSE)
                {
                    NextToken(json, ref index);

                    return table;
                }
                else
                {
                    // name
                    string name = ParseString(json, ref index, ref success);

                    if (!success)
                    {
                        success = false;

                        return null;
                    }

                    // :
                    token = NextToken(json, ref index);

                    if (token != Json.TOKEN_COLON)
                    {
                        success = false;
                        return null;
                    }

                    // value
                    object value = ParseValue(json, ref index, ref success);

                    if (!success)
                    {
                        success = false;

                        return null;
                    }

                    table.Add(name, value);
                }
            }

            return table;
        }

        protected static object[] ParseArray(char[] json, ref int index, ref bool success)
        {
            List<object> array = new List<object>();

            // [
            NextToken(json, ref index);

            bool done = false;

            while (!done)
            {
                int token = LookAhead(json, index);

                if (token == Json.TOKEN_NONE)
                {
                    success = false;

                    return null;
                }
                else if (token == Json.TOKEN_COMMA)
                {
                    NextToken(json, ref index);
                }
                else if (token == Json.TOKEN_SQUARED_CLOSE)
                {
                    NextToken(json, ref index);

                    break;
                }
                else
                {
                    object value = ParseValue(json, ref index, ref success);

                    if (!success)
                    {
                        return null;
                    }

                    array.Add(value);
                }
            }

            return array.ToArray();
        }

        protected static object ParseValue(char[] json, ref int index, ref bool success)
        {
            switch (LookAhead(json, index))
            {
                case Json.TOKEN_STRING:
                    return ParseString(json, ref index, ref success);
                case Json.TOKEN_NUMBER:
                    return ParseNumber(json, ref index, ref success);
                case Json.TOKEN_CURLY_OPEN:
                    return ParseObject(json, ref index, ref success);
                case Json.TOKEN_SQUARED_OPEN:
                    return ParseArray(json, ref index, ref success);
                case Json.TOKEN_TRUE:
                    NextToken(json, ref index);
                    return true;
                case Json.TOKEN_FALSE:
                    NextToken(json, ref index);
                    return false;
                case Json.TOKEN_NULL:
                    NextToken(json, ref index);
                    return null;
                case Json.TOKEN_NONE:
                    break;
            }

            success = false;
            return null;
        }

        protected static string ParseString(char[] json, ref int index, ref bool success)
        {
            StringBuilder s = new StringBuilder();
            char c;

            EatWhitespace(json, ref index);

            // "
            c = json[index++];

            bool complete = false;
            while (!complete)
            {
                if (index == json.Length)
                {
                    break;
                }

                c = json[index++];

                if (c == '"')
                {
                    complete = true;

                    break;
                }
                else if (c == '\\')
                {

                    if (index == json.Length)
                    {
                        break;
                    }

                    c = json[index++];

                    if (c == '"')
                    {
                        s.Append('"');
                    }
                    else if (c == '\\')
                    {
                        s.Append('\\');
                    }
                    else if (c == '/')
                    {
                        s.Append('/');
                    }
                    else if (c == 'b')
                    {
                        s.Append('\b');
                    }
                    else if (c == 'f')
                    {
                        s.Append('\f');
                    }
                    else if (c == 'n')
                    {
                        s.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        s.Append('\r');
                    }
                    else if (c == 't')
                    {
                        s.Append('\t');
                    }
                    else if (c == 'u')
                    {
                        int remainingLength = json.Length - index;

                        if (remainingLength >= 4)
                        {
                            // parse the 32 bit hex into an integer codepoint
                            uint codePoint;

                            if (!(success = UInt32.TryParse(new string(json, index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint)))
                            {
                                return String.Empty;
                            }

                            // convert the integer codepoint to a unicode char and add to string
                            s.Append(Encoding.UTF32.GetString(BitConverter.GetBytes(codePoint)));
                            // skip 4 chars
                            index += 4;
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                else
                {
                    s.Append(c);
                }

            }

            if (!complete)
            {
                success = false;

                return null;
            }

            return s.ToString();
        }

        protected static double ParseNumber(char[] json, ref int index, ref bool success)
        {
            EatWhitespace(json, ref index);

            int lastIndex = GetLastIndexOfNumber(json, index);
            int charLength = (lastIndex - index) + 1;

            double number;
            success = Double.TryParse(new string(json, index, charLength), NumberStyles.Any, CultureInfo.InvariantCulture, out number);

            index = lastIndex + 1;

            return number;
        }

        protected static int GetLastIndexOfNumber(char[] json, int index)
        {
            int lastIndex;

            for (lastIndex = index; lastIndex < json.Length; lastIndex++)
            {
                if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
                {
                    break;
                }
            }
            return lastIndex - 1;
        }

        protected static void EatWhitespace(char[] json, ref int index)
        {
            for (; index < json.Length; index++)
            {
                if (" \t\n\r".IndexOf(json[index]) == -1)
                {
                    break;
                }
            }
        }

        protected static int LookAhead(char[] json, int index)
        {
            int saveIndex = index;
            return NextToken(json, ref saveIndex);
        }

        protected static int NextToken(char[] json, ref int index)
        {
            EatWhitespace(json, ref index);

            if (index == json.Length)
            {
                return Json.TOKEN_NONE;
            }

            char c = json[index];
            index++;

            switch (c)
            {
                case '{':
                    return Json.TOKEN_CURLY_OPEN;
                case '}':
                    return Json.TOKEN_CURLY_CLOSE;
                case '[':
                    return Json.TOKEN_SQUARED_OPEN;
                case ']':
                    return Json.TOKEN_SQUARED_CLOSE;
                case ',':
                    return Json.TOKEN_COMMA;
                case '"':
                    return Json.TOKEN_STRING;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    return Json.TOKEN_NUMBER;
                case ':':
                    return Json.TOKEN_COLON;
            }

            index--;

            int remainingLength = json.Length - index;

            // false
            if (remainingLength >= 5)
            {
                if (json[index] == 'f' &&
                    json[index + 1] == 'a' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 's' &&
                    json[index + 4] == 'e')
                {
                    index += 5;

                    return Json.TOKEN_FALSE;
                }
            }

            // true
            if (remainingLength >= 4)
            {
                if (json[index] == 't' &&
                    json[index + 1] == 'r' &&
                    json[index + 2] == 'u' &&
                    json[index + 3] == 'e')
                {
                    index += 4;

                    return Json.TOKEN_TRUE;
                }
            }

            // null
            if (remainingLength >= 4)
            {
                if (json[index] == 'n' &&
                    json[index + 1] == 'u' &&
                    json[index + 2] == 'l' &&
                    json[index + 3] == 'l')
                {
                    index += 4;

                    return Json.TOKEN_NULL;
                }
            }

            return Json.TOKEN_NONE;
        }

        protected static bool SerializeValue(object value, StringBuilder builder)
        {
            bool success = true;

            if (value is string)
            {
                success = SerializeString((string)value, builder);
            }
            else if (value is Dictionary<string, object>)
            {
                success = SerializeObject((Dictionary<string, object>)value, builder);
            }
            else if (value is object[])
            {
                success = SerializeArray((object[])value, builder);
            }
            else if ((value is Boolean) && ((Boolean)value == true))
            {
                builder.Append("true");
            }
            else if ((value is Boolean) && ((Boolean)value == false))
            {
                builder.Append("false");
            }
            else if (value is ValueType)
            {
                // thanks to ritchie for pointing out ValueType to me
                success = SerializeNumber(Convert.ToDouble(value), builder);
            }
            else if (value == null)
            {
                builder.Append("null");
            }
            else
            {
                success = false;
            }
            return success;
        }

        protected static bool SerializeObject(Dictionary<string, object> anObject, StringBuilder builder)
        {
            builder.Append("{");

            bool first = true;

            foreach (KeyValuePair<string, object> kvp in anObject)
            {
                string key = kvp.Key.ToString();
                object value = kvp.Value;

                if (!first)
                {
                    builder.Append(", ");
                }

                SerializeString(key, builder);
                builder.Append(":");

                if (!SerializeValue(value, builder))
                {
                    return false;
                }

                first = false;
            }

            builder.Append("}");

            return true;
        }

        protected static bool SerializeArray(object[] anArray, StringBuilder builder)
        {
            builder.Append("[");

            bool first = true;

            for (int i = 0; i < anArray.Length; i++)
            {
                object value = anArray[i];

                if (!first)
                {
                    builder.Append(", ");
                }

                if (!SerializeValue(value, builder))
                {
                    return false;
                }

                first = false;
            }

            builder.Append("]");

            return true;
        }

        protected static bool SerializeString(string aString, StringBuilder builder)
        {
            builder.Append("\"");

            char[] charArray = aString.ToCharArray();

            for (int i = 0; i < charArray.Length; i++)
            {
                char c = charArray[i];
                if (c == '"')
                {
                    builder.Append("\\\"");
                }
                else if (c == '\\')
                {
                    builder.Append("\\\\");
                }
                else if (c == '\b')
                {
                    builder.Append("\\b");
                }
                else if (c == '\f')
                {
                    builder.Append("\\f");
                }
                else if (c == '\n')
                {
                    builder.Append("\\n");
                }
                else if (c == '\r')
                {
                    builder.Append("\\r");
                }
                else if (c == '\t')
                {
                    builder.Append("\\t");
                }
                else
                {
                    int codepoint = Convert.ToInt32(c);

                    if ((codepoint >= 32) && (codepoint <= 126))
                    {
                        builder.Append(c);
                    }
                    else
                    {
                        builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                    }
                }
            }

            builder.Append("\"");

            return true;
        }

        protected static bool SerializeNumber(double number, StringBuilder builder)
        {
            builder.Append(Convert.ToString(number, CultureInfo.InvariantCulture));

            return true;
        }
    }

    internal class Utility
    {
        public static string UrlEncode(string s)
        {
            const string unreserved = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
            var sb = new StringBuilder();

            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                if (b < 0x80 && unreserved.IndexOf(Convert.ToChar(b)) != -1)
                {
                    sb.Append(Convert.ToChar(b));
                }
                else
                {
                    sb.Append('%' + String.Format("{0:X2}", Convert.ToInt32(b)));
                }
            }

            return sb.ToString();
        }
    }
}
