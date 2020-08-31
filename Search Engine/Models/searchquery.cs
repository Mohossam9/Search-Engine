using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Search_Engine.Models
{
    public class searchquery
    {
        public string query { get; set; }
        public string [] Query_to_words()
        {
            char[] split_token = { ' ', ',', '!', '@', '#', '&', '“', '”', '‘', '—', '(', ')', '–', '[', '{', '}', ']', ':', ';', '?', '/', '*', '.', '-', '"', '\'', '\\', '%', '$', '^', '\r', '\n', '~', '`', '’', ',', 'ـ', '_' };
            string searchquery = query.ToLower();
            string[] searchquery_terms = searchquery.Split(split_token);
            return searchquery_terms;
        }
    }
}