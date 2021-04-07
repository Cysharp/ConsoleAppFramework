//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace ConsoleAppFramework
//{
//    public interface INamingConverter
//    {
//        string ConvertToAliasName(string name);
//        string ConvertToOriginalName(string name);
//    }

//    public class OriginalNamingConverter : INamingConverter
//    {
//        public string ConvertToAliasName(string name)
//        {
//            return name;
//        }

//        public string ConvertToOriginalName(string name)
//        {
//            return name;
//        }
//    }

//    public class LowerCaseNamingConverter : INamingConverter
//    {
//        public string ConvertToAliasName(string name)
//        {
//            return name.ToLower();
//        }

//        // compare is case insensitive so use as is.
//        public string ConvertToOriginalName(string name)
//        {
//            return name;
//        }
//    }

//    public class HypenLowerNamingConverter : INamingConverter
//    {
//        public string ConvertToAliasName(string s)
//        {
//            if (string.IsNullOrEmpty(s)) return s;

//            var sb = new StringBuilder();
//            for (int i = 0; i < s.Length; i++)
//            {
//                var c = s[i];

//                if (Char.IsUpper(c))
//                {
//                    // first
//                    if (i == 0)
//                    {
//                        sb.Append(char.ToLowerInvariant(c));
//                    }
//                    else if (char.IsUpper(s[i - 1])) // WriteIO => write-io
//                    {
//                        sb.Append(char.ToLowerInvariant(c));
//                    }
//                    else
//                    {
//                        sb.Append("-");
//                        sb.Append(char.ToLowerInvariant(c));
//                    }
//                }
//                else
//                {
//                    sb.Append(c);
//                }
//            }

//            return sb.ToString();
//        }

//        public string ConvertToOriginalName(string s)
//        {
//            var sb = new StringBuilder();
//            for (int i = 0; i < s.Length; i++)
//            {
//                if (s[i] == '-')
//                {
//                    continue;
//                }
//                else
//                {
//                    sb.Append(s[i]);
//                }
//            }

//            return sb.ToString();
//        }
//    }
//}
