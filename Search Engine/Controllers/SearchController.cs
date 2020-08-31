using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Search_Engine.Models;
using System.Data.SqlClient;
using System.Data;

namespace Search_Engine.Controllers
{
    public class SearchController : Controller
    {
        String misSpelledword = "";
        

        // GET: Search
        public ActionResult Search()
        {
            return View();
        }

        public ActionResult geturls(string searchterms,string type)
        {
            searchquery s = new searchquery();
            s.query = searchterms;
            string[] searchquery_terms = s.Query_to_words();
     
            char[] searchquery_arr = searchterms.ToCharArray();
            Dictionary<Tuple<string, int>, Tuple<int, List<int>>> dict = new Dictionary<Tuple<string, int>, Tuple<int, List<int>>>();
            Dictionary<String, int> Ndocs = new Dictionary<string, int>();

            bool exact_search = false;
            List<int> Doc_No = new List<int>();
            if (searchquery_arr[0] == '"' && searchquery_arr[searchquery_arr.Length - 1] == '"')
            {
                s.query = searchterms.Substring(1, searchterms.Length - 2);
                exact_search = true;
            }
            List<string> searchquery_terms_stemmed = new List<string>();

            SqlConnection con = new SqlConnection(@"Data Source=HOSSAM\MOHAMEDHOSSAM;Initial Catalog=web_crawler;Integrated Security=True");
            con.Open();

            for (int i = 0; i < searchquery_terms.Length; i++)
            {
                if (!Remove_stopwords(searchquery_terms[i]))
                {
                    Porter stemer = new Porter(); //object from porter stemmer
                    string output = stemer.stem(searchquery_terms[i]); //pass the term for the stemmer to apply the porter stemmer on it
                    searchquery_terms_stemmed.Add(output);

                    
                    SqlCommand cmd = new SqlCommand("select * from Inverted_index where Term=@term", con);
                    // SqlParameter term = new SqlParameter("@term",output);//current term in dictionary
                    // cmd.Parameters.Add(term);//select all records 'page content' from crawler database
                    cmd.Parameters.Add("@term", SqlDbType.VarChar).Value = output;
                    SqlDataReader reader = cmd.ExecuteReader();  //reader on database
                    if (!reader.HasRows && type=="spell")
                    {
                        misSpelledword = searchquery_terms[i];
                    }
                    else
                    {
                       
                        int repeation = 0;
                        while (reader.Read())
                        {
                            string word = reader[0].ToString();
                            int doc_id = (int)reader[1];
                            if (!Doc_No.Contains(doc_id))
                            {
                                Doc_No.Add(doc_id);
                            }
                            int frequency = (int)reader[2];
                            string positions = reader[3].ToString();
                            List<int> term_positions = new List<int>();
                            string[] arr = positions.Split(',');
                            int[] position = Array.ConvertAll(arr, int.Parse);

                            for (int j = 0; j < position.Length; j++)
                            {
                                term_positions.Add(position[j]);
                            }
                            dict.Add(Tuple.Create(word, doc_id), Tuple.Create(frequency, term_positions));
                            repeation++;

                        }
                        Ndocs.Add(output, repeation);
                    }
                    reader.Close();
                }   
            }

            List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> Exact_Docs = new List<Tuple<int, float, List<Tuple<string, int, List<int>>>>>();
            List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> Inexact_Docs = new List<Tuple<int, float, List<Tuple<string, int, List<int>>>>>();
            List<String> Urls = new List<string>();

            if (exact_search)
            {
                List<Tuple<int, List<Tuple<String, int, List<int>>>>> docs = new List<Tuple<int, List<Tuple<String, int, List<int>>>>>();
                for (int i = 0; i < Doc_No.Count; i++)
                {
                    int count = 0, frequency;
                    List<int> allpositions = new List<int>();
                    List<Tuple<String, int, List<int>>> words = new List<Tuple<string, int, List<int>>>();

                    for (int j = 0; j < searchquery_terms_stemmed.Count; j++)
                    {
                        if (dict.ContainsKey(Tuple.Create(searchquery_terms_stemmed[j], Doc_No[i])))
                        {
                            count++;
                            allpositions = dict[Tuple.Create(searchquery_terms_stemmed[j], Doc_No[i])].Item2;
                            frequency = dict[Tuple.Create(searchquery_terms_stemmed[j], Doc_No[i])].Item1;
                            words.Add(Tuple.Create(searchquery_terms_stemmed[j], frequency, allpositions));
                        }
                    }
                    if (count == searchquery_terms_stemmed.Count)
                    {
                        docs.Add(Tuple.Create(Doc_No[i], words));
                    }
                }

                List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> Docs_distances = check_distance(docs, searchquery_terms_stemmed.Count, Ndocs);
                foreach (var item in Docs_distances.OrderByDescending(Key => Key.Item2))
                {
                    Exact_Docs.Add(item);
                }

                Urls=Read_URls_from_database(Exact_Docs);

            }
            else
            {
                Dictionary<int, List<Tuple<int, List<Tuple<String, int, List<int>>>>>> num_of_occurence = new Dictionary<int, List<Tuple<int, List<Tuple<String, int, List<int>>>>>>();
                for (int i = 0; i < Doc_No.Count; i++)
                {
                    int count = 0, frequency;
                    List<int> allpositions = new List<int>();
                    List<Tuple<String, int, List<int>>> word = new List<Tuple<string, int, List<int>>>();

                    for (int j = 0; j < searchquery_terms_stemmed.Count; j++)
                    {
                        if (dict.ContainsKey(Tuple.Create(searchquery_terms_stemmed[j], Doc_No[i])))
                        {
                            count++;
                            allpositions = dict[Tuple.Create(searchquery_terms_stemmed[j], Doc_No[i])].Item2;
                            frequency = dict[Tuple.Create(searchquery_terms_stemmed[j], Doc_No[i])].Item1;
                            word.Add(Tuple.Create(searchquery_terms_stemmed[j], frequency, allpositions));
                        }
                    }
                    if (num_of_occurence.ContainsKey(count))
                        num_of_occurence[count].Add(Tuple.Create(Doc_No[i], word));
                    else
                    {
                        List<Tuple<int, List<Tuple<String, int, List<int>>>>> alldocs = new List<Tuple<int, List<Tuple<String, int, List<int>>>>>();
                        alldocs.Add(Tuple.Create(Doc_No[i], word));
                        num_of_occurence.Add(count, alldocs);
                    }

                }

                foreach (var Item in num_of_occurence.OrderByDescending(key => key.Key))
                {

                    List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> docs_distances = calculate_distance(Item.Value, Item.Key);
                    foreach (var item in docs_distances.OrderBy(key => key.Item2))
                        Inexact_Docs.Add(item);
                }
                Urls=Read_URls_from_database(Inexact_Docs);
                if (misSpelledword != "" && type=="spell")
                {
                    List<String> nearest_words = spellchecker_words(misSpelledword);
                    ViewBag.nearest_words = nearest_words;
                }
                else if(type=="soundex")
                {
                    misSpelledword = searchquery_terms[0];
                    List<String> soundex_words = Soundex_words(misSpelledword);
                    ViewBag.nearest_words = soundex_words;
                }
                
            }

            ViewBag.Urls = Urls;
            ViewBag.query = searchterms;
            ViewBag.type = type;
            return View();
        }


        public List<string> spellchecker_words(String misspelled)
        {
            List<String> miss_grams = K_gram(misspelled, 2);
            List<String> nearwords = new List<string>();

            SqlConnection con = new SqlConnection(@"Data Source=HOSSAM\MOHAMEDHOSSAM;Initial Catalog=web_crawler;Integrated Security=True");
            con.Open();

            for (int i = 0; i < miss_grams.Count; i++)
            {
                SqlCommand cmd = new SqlCommand("select Term from kgram_index where gram=@gram", con);
                SqlParameter gram = new SqlParameter("@gram", miss_grams[i]);
                cmd.Parameters.Add(gram);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (!nearwords.Contains(reader[0].ToString()))
                        nearwords.Add(reader[0].ToString());
                }
                reader.Close();
            }
            con.Close();

            Dictionary<String, float> nearwords_coffs = new Dictionary<string, float>();
            for (int i = 0; i < nearwords.Count; i++)
            {
                int common = 0;
                List<String> nearword_grams = K_gram(nearwords[i], 2);
                for (int j = 0; j < miss_grams.Count; j++)
                {
                    if (nearword_grams.Contains(miss_grams[j]))
                        common++;
                }
                float coff = (float)2 * ((float)(common) / (float)(miss_grams.Count + nearword_grams.Count));
                if (coff > 0.45)
                    nearwords_coffs.Add(nearwords[i], coff);
            }

            Dictionary<string, int> nearwords_dis = new Dictionary<string, int>();
            for (int i = 0; i < nearwords_coffs.Count; i++)
            {
                int dis = Edit_Distance(nearwords_coffs.ElementAt(i).Key, misspelled, nearwords_coffs.ElementAt(i).Key.Length, misspelled.Length);
                nearwords_dis.Add(nearwords_coffs.ElementAt(i).Key, dis);
            }

            int min = int.MaxValue;
            Boolean firsttime = true;
            List<string> nearest_words = new List<string>();
            foreach (var word in nearwords_dis.OrderBy(key => key.Value))
            {
                if (firsttime)
                {
                    min = word.Value;
                    firsttime = false;
                    nearest_words.Add(word.Key);
                }
                else if (word.Value == min)
                    nearest_words.Add(word.Key);
                else
                    break;
            }
            return nearest_words;
        }
        public List<String> Soundex_words(String misspelled)
        {
            List<char> res = Soundx_algorithm(misSpelledword);
            string soundex_hash = "";
            for (int i = 0; i < res.Count; i++)
            {
                soundex_hash += res[i];
            }
            List<string> Same_Soundex_Hash = new List<string>();
            SqlConnection connection = new SqlConnection(@"Data Source=HOSSAM\MOHAMEDHOSSAM;Initial Catalog=web_crawler;Integrated Security=True");
            connection.Open();
            SqlCommand cmd = new SqlCommand("select term from Soundex_Index where phonetic_hash =@soundex_hash", connection);
            SqlParameter hash = new SqlParameter("@soundex_hash", soundex_hash);
            cmd.Parameters.Add(hash);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string term = reader[0].ToString();
                Same_Soundex_Hash.Add(term);

            }
            reader.Close();
            connection.Close();
            return Same_Soundex_Hash;
        }
        public List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> check_distance(List<Tuple<int, List<Tuple<String, int, List<int>>>>> docs, int count, Dictionary<String, int> Ndocs)
        {
            List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> frequencies = new List<Tuple<int, float, List<Tuple<string, int, List<int>>>>>();

            for (int doc = 0; doc < docs.Count; doc++)
            {
                float frequency = 0;
                int check_diff = 0;

                if (count == 1)
                {
                    string word_name = docs.ElementAt(doc).Item2.ElementAt(0).Item1;
                    frequency = (float)docs.ElementAt(doc).Item2.ElementAt(0).Item2 / (float)Ndocs[word_name];
                    frequencies.Add(Tuple.Create(docs.ElementAt(doc).Item1, frequency, docs.ElementAt(doc).Item2));

                }
                else
                {
                    int next_word = 1;
                    int curr_index = docs.ElementAt(doc).Item2.ElementAt(0).Item3.Count;
                    Boolean continuous = false;


                    for (int index1 = 0; index1 < curr_index; index1++)
                    {
                        int curr_index2 = docs.ElementAt(doc).Item2.ElementAt(next_word).Item3.Count;
                        int pos1 = docs.ElementAt(doc).Item2.ElementAt(0).Item3.ElementAt(index1);
                        for (int index2 = 0; index2 < curr_index2;)
                        {
                            int pos2 = docs.ElementAt(doc).Item2.ElementAt(next_word).Item3.ElementAt(index2);
                            int dis = pos1 - pos2;

                            if (dis == -1)
                            {
                                check_diff++;
                                pos1 = docs.ElementAt(doc).Item2.ElementAt(next_word).Item3.ElementAt(index2);
                                String word_name = docs.ElementAt(doc).Item2.ElementAt(next_word - 1).Item1;
                                frequency += (float)docs.ElementAt(doc).Item2.ElementAt(next_word - 1).Item2 / (float)Ndocs[word_name];
                                next_word++;
                                index2 = 0;
                                if (next_word == count && check_diff == count - 1)
                                {
                                    word_name = docs.ElementAt(doc).Item2.ElementAt(next_word - 1).Item1;
                                    frequency += (float)docs.ElementAt(doc).Item2.ElementAt(next_word - 1).Item2 / (float)Ndocs[word_name];
                                    frequencies.Add(Tuple.Create(docs.ElementAt(doc).Item1, frequency, docs.ElementAt(doc).Item2));
                                    continuous = true;
                                    break;
                                }
                                else if (next_word == count)
                                {
                                    check_diff = 0;
                                    next_word = 1;
                                    frequency = 0;
                                    break;
                                }
                                curr_index2 = docs.ElementAt(doc).Item2.ElementAt(next_word).Item3.Count;
                            }
                            else
                                index2++;
                        }
                        if (continuous)
                            break;
                    }
                }


            }
            frequencies = frequencies.OrderBy(t => t.Item2).ToList();
            return frequencies;
        }
        public List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> calculate_distance(List<Tuple<int, List<Tuple<String, int, List<int>>>>> docs, int count)
        {
            List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> distances = new List<Tuple<int, float, List<Tuple<string, int, List<int>>>>>();

            for (int doc = 0; doc < docs.Count; doc++)
            {
                int min_sum = int.MaxValue;
                for (int word = 0; word < count; word++)
                {
                    int sum = 0;
                    int counter = 0;

                    if (count == 1)
                    {
                        min_sum = docs.ElementAt(doc).Item2.ElementAt(word).Item3.ElementAt(0);
                        break;
                    }
                    else if (count == 2)
                    {
                        counter = 1;
                    }
                    for (int nextword = word + 1; counter < count; nextword++)
                    {
                        int min = int.MaxValue;
                        int next_word = nextword % count;
                        int curr_index = docs.ElementAt(doc).Item2.ElementAt(word).Item3.Count;
                        int curr_index2 = docs.ElementAt(doc).Item2.ElementAt(next_word).Item3.Count;

                        for (int index1 = 0; index1 < curr_index; index1++)
                        {
                            for (int index2 = 0; index2 < curr_index2; index2++)
                            {

                                int pos1 = docs.ElementAt(doc).Item2.ElementAt(word).Item3.ElementAt(index1);
                                int pos2 = docs.ElementAt(doc).Item2.ElementAt(next_word).Item3.ElementAt(index2);
                                int dis = Math.Abs(pos1 - pos2);

                                if (dis < min)
                                    min = dis;
                            }
                        }
                        sum += min;
                        counter++;
                    }
                    if (min_sum > sum)
                        min_sum = sum;


                }
                distances.Add(Tuple.Create(docs.ElementAt(doc).Item1, (float)min_sum, docs.ElementAt(doc).Item2));
            }

            distances = distances.OrderBy(t => t.Item2).ToList();
            return distances;
        }

        public List<String> K_gram(String word, int k)
        {
            List<String> grams = new List<string>();
            word = '$' + word + '$';
            bool end = false;
            for (int i = 0; i < word.Length; i++)
            {
                String gram = "";
                for (int j = i; j < k + i && !end; j++)
                {
                    gram += word[j];
                    if (word[j] == '$' && i != 0)
                        end = true;
                }
                grams.Add(gram);
                if (end)
                    break;
            }
            return grams;
        }
        public static List<char> Soundx_algorithm(string word)
        {
            List<char> Replace_by_zero = new List<char> { 'a', 'e', 'i', 'o', 'u', 'h', 'w', 'y' };
            List<char> Replace_by_one = new List<char> { 'b', 'f', 'p', 'v' };
            List<char> Replace_by_two = new List<char> { 'c', 'g', 'j', 'k', 'q', 's', 'x', 'z' };
            List<char> Replace_by_Three = new List<char> { 'd', 't' };
            List<char> Replace_by_four = new List<char> { 'l' };
            List<char> Replace_by_five = new List<char> { 'm', 'n' };
            List<char> Replace_by_six = new List<char> { 'r' };
            char[] term = word.ToCharArray();
            List<char> phonatic_hash = new List<char> { };
            for (int i = 0; i < term.Length; i++)
            {
                if (i == 0)
                {
                    phonatic_hash.Add(term[i]);
                }
                else
                {
                    if (Replace_by_zero.Contains(term[i]))
                    {
                        phonatic_hash.Add('0');
                    }
                    else if (Replace_by_one.Contains(term[i]))
                    {

                        phonatic_hash.Add('1');
                    }
                    else if (Replace_by_two.Contains(term[i]))
                    {
                        phonatic_hash.Add('2');
                    }
                    else if (Replace_by_Three.Contains(term[i]))
                    {
                        phonatic_hash.Add('3');
                    }
                    else if (Replace_by_four.Contains(term[i]))
                    {
                        phonatic_hash.Add('4');
                    }
                    else if (Replace_by_five.Contains(term[i]))
                    {
                        phonatic_hash.Add('5');
                    }
                    else if (Replace_by_six.Contains(term[i]))
                    {
                        phonatic_hash.Add('6');
                    }

                }
            }

            for (int j = 1; j < phonatic_hash.Count; j++)
            {

                if (j != phonatic_hash.Count - 1)
                {

                    if (phonatic_hash[j] == phonatic_hash[j + 1])
                    {
                        phonatic_hash.RemoveAt(j);
                    }
                }

            }
            for (int j = 0; j < phonatic_hash.Count; j++)
            {


                if (phonatic_hash[j] == '0')
                {
                    phonatic_hash.RemoveAt(j);
                }


            }
            phonatic_hash.Remove('0');

            if (phonatic_hash.Count == 4)
            {
                return phonatic_hash;
            }
            else if (phonatic_hash.Count < 4)
            {
                int No_zeros_to_append = 4 - phonatic_hash.Count;
                for (int i = 0; i < No_zeros_to_append; i++)
                {
                    phonatic_hash.Add('0');
                }

            }
            else if (phonatic_hash.Count > 4)
            {
                int No_char_to_be_remove = phonatic_hash.Count - 4;
                phonatic_hash.RemoveRange(4, No_char_to_be_remove);

            }

            return phonatic_hash;
        }
        public int min(int a, int b)
        {
            return (a < b) ? a : b;
        }
        public int min(int x, int y, int z)
        {
            return min(min(x, y), z);
        }
        public int Edit_Distance(String word1, String word2, int word1_length, int word2_length)
        {

            // If second string is empty , return count of first
            if (word2_length == 0)
                return word1_length;

            // If first string is empty ,return count of second
            if (word1_length == 0)
                return word2_length;

            // If last characters of two strings are same, continue
            if (word1[word1_length - 1] == word2[word2_length - 1])
                return Edit_Distance(word1, word2, word1_length - 1, word2_length - 1);

            // If last characters are not same, consider all three 
            return 1 + min(Edit_Distance(word1, word2, word1_length, word2_length - 1), Edit_Distance(word1, word2, word1_length - 1, word2_length), Edit_Distance(word1, word2, word1_length - 1, word2_length - 1));
        }  
        public List<String> Read_URls_from_database(List<Tuple<int, float, List<Tuple<String, int, List<int>>>>> docs)
        {
            List<String> Urls = new List<string>();
            SqlConnection con = new SqlConnection(@"Data Source=HOSSAM\MOHAMEDHOSSAM;Initial Catalog=web_crawler;Integrated Security=True");
            con.Open();

            int counter = 0;
            while (counter != docs.Count)
            {
                SqlCommand cmd = new SqlCommand("select Url from Crawler where ID=@id", con);
                // SqlParameter term = new SqlParameter("@term",output);//current term in dictionary
                // cmd.Parameters.Add(term);//select all records 'page content' from crawler database
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = docs.ElementAt(counter).Item1;
                SqlDataReader reader = cmd.ExecuteReader();  //reader on database
                while (reader.Read())
                {
                    Urls.Add(reader[0].ToString());
                }
                reader.Close();
                counter++;
            }
            con.Close();
            return Urls;
        }
        //dictionary contains stop words 
        static Dictionary<string, bool> stop_words = new Dictionary<string, bool>
            {
        { "about", true },
        { "above", true },
        { "across", true },
        { "after", true },
        { "afterwards", true },
        { "again", true },
        { "against", true },
        { "all", true },
        { "almost", true },
        { "alone", true },
        { "along", true },
        { "already", true },
        { "also", true },
        { "although", true },
        { "always", true },
        { "among", true },
        { "amongst", true },
        { "amount", true },
        { "and", true },
        { "another", true },
        { "any", true },
        { "anyhow", true },
        { "anyone", true },
        { "anything", true },
        { "anyway", true },
        { "anywhere", true },
        { "are", true },
        { "around", true },
        { "back", true },
        { "became", true },
        { "because", true },
        { "become", true },
        { "becomes", true },
        { "becoming", true },
        { "been", true },
        { "before", true },
        { "beforehand", true },
        { "behind", true },
        { "being", true },
        { "below", true },
        { "beside", true },
        { "besides", true },
        { "between", true },
        { "beyond", true },
        { "bill", true },
        { "both", true },
        { "bottom", true },
        { "but", true },
        { "call", true },
        { "can", true },
        { "cannot", true },
        { "cant", true },
        { "computer", true },
        { "con", true },
        { "could", true },
        { "couldnt", true },
        { "cry", true },
        { "describe", true },
        { "detail", true },
        { "done", true },
        { "down", true },
        { "due", true },
        { "during", true },
        { "each", true },
        { "eight", true },
        { "either", true },
        { "eleven", true },
        { "else", true },
        { "elsewhere", true },
        { "empty", true },
        { "enough", true },
        { "etc", true },
        { "even", true },
        { "ever", true },
        { "every", true },
        { "everyone", true },
        { "everything", true },
        { "everywhere", true },
        { "except", true },
        { "few", true },
        { "fifteen", true },
        { "fify", true },
        { "fill", true },
        { "find", true },
        { "fire", true },
        { "first", true },
        { "five", true },
        { "for", true },
        { "former", true },
        { "formerly", true },
        { "forty", true },
        { "found", true },
        { "four", true },
        { "from", true },
        { "front", true },
        { "full", true },
        { "further", true },
        { "get", true },
        { "give", true },
        { "had", true },
        { "has", true },
        { "have", true },
        { "hence", true },
        { "her", true },
        { "here", true },
        { "hereafter", true },
        { "hereby", true },
        { "herein", true },
        { "hereupon", true },
        { "hers", true },
        { "herself", true },
        { "him", true },
        { "himself", true },
        { "his", true },
        { "how", true },
        { "however", true },
        { "hundred", true },
        { "inc", true },
        { "indeed", true },
        { "interest", true },
        { "into", true },
        { "its", true },
        { "itself", true },
        { "keep", true },
        { "last", true },
        { "latter", true },
        { "latterly", true },
        { "least", true },
        { "less", true },
        { "ltd", true },
        { "made", true },
        { "many", true },
        { "may", true },
        { "meanwhile", true },
        { "might", true },
        { "mill", true },
        { "mine", true },
        { "more", true },
        { "moreover", true },
        { "most", true },
        { "mostly", true },
        { "move", true },
        { "much", true },
        { "must", true },
        { "myself", true },
        { "name", true },
        { "namely", true },
        { "neither", true },
        { "never", true },
        { "nevertheless", true },
        { "next", true },
        { "nine", true },
        { "nobody", true },
        { "none", true },
        { "nor", true },
        { "not", true },
        { "nothing", true },
        { "now", true },
        { "nowhere", true },
        { "off", true },
        { "often", true },
        { "once", true },
        { "one", true },
        { "only", true },
        { "onto", true },
        { "other", true },
        { "others", true },
        { "otherwise", true },
        { "our", true },
        { "ours", true },
        { "ourselves", true },
        { "out", true },
        { "over", true },
        { "own", true },
        { "part", true },
        { "per", true },
        { "perhaps", true },
        { "please", true },
        { "put", true },
        { "rather", true },
        { "same", true },
        { "see", true },
        { "seem", true },
        { "seemed", true },
        { "seeming", true },
        { "seems", true },
        { "serious", true },
        { "several", true },
        { "she", true },
        { "should", true },
        { "show", true },
        { "side", true },
        { "since", true },
        { "sincere", true },
        { "six", true },
        { "sixty", true },
        { "some", true },
        { "somehow", true },
        { "someone", true },
        { "something", true },
        { "sometime", true },
        { "sometimes", true },
        { "somewhere", true },
        { "still", true },
        { "such", true },
        { "system", true },
        { "take", true },
        { "ten", true },
        { "than", true },
        { "that", true },
        { "the", true },
        { "their", true },
        { "them", true },
        { "themselves", true },
        { "then", true },
        { "thence", true },
        { "there", true },
        { "thereafter", true },
        { "thereby", true },
        { "therefore", true },
        { "therein", true },
        { "thereupon", true },
        { "these", true },
        { "they", true },
        { "thick", true },
        { "thin", true },
        { "third", true },
        { "this", true },
        { "those", true },
        { "though", true },
        { "three", true },
        { "through", true },
        { "throughout", true },
        { "thru", true },
        { "thus", true },
        { "together", true },
        { "too", true },
        { "top", true },
        { "toward", true },
        { "towards", true },
        { "twelve", true },
        { "twenty", true },
        { "two", true },
        { "under", true },
        { "until", true },
        { "upon", true },
        { "very", true },
        { "via", true },
        { "was", true },
        { "well", true },
        { "were", true },
        { "what", true },
        { "whatever", true },
        { "when", true },
        { "whence", true },
        { "whenever", true },
        { "where", true },
        { "whereafter", true },
        { "whereas", true },
        { "whereby", true },
        { "wherein", true },
        { "whereupon", true },
        { "wherever", true },
        { "whether", true },
        { "which", true },
        { "while", true },
        { "whither", true },
        { "who", true },
        { "whoever", true },
        { "whole", true },
        { "whom", true },
        { "whose", true },
        { "why", true },
        { "will", true },
        { "with", true },
        { "within", true },
        { "without", true },
        { "would", true },
        { "yet", true },
        { "you", true },
        { "your", true },
        { "yours", true },
        { "yourself", true },
        { "yourselves", true }
    };
        //stopword checker funtion
        public static bool Remove_stopwords(string words) //bool function to check if the term is stop word or not
        {
            if (words.Length < 3) // if term length < 3 remove it
            {
                return true;
            }
            if (stop_words.ContainsKey(words)) // if term is stop word remove it
            {
                return true;
            }
            return false;

        }
    }
}