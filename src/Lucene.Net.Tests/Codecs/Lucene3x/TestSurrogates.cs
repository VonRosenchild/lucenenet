using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Codecs.Lucene3x
{
    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements.  See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License.  You may obtain a copy of the License at
         *
         *     http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS,
         * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
         * See the License for the specific language governing permissions and
         * limitations under the License.
         */

    [TestFixture]
    public class TestSurrogates : LuceneTestCase
    {
        /// <summary>
        /// we will manually instantiate preflex-rw here
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
        }

        private static string MakeDifficultRandomUnicodeString(Random r)
        {
            int end = r.Next(20);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            for (int i = 0; i < end; i++)
            {
                int t = r.Next(5);

                if (0 == t && i < end - 1)
                {
                    // hi
                    buffer[i++] = (char)(0xd800 + r.Next(2));
                    // lo
                    buffer[i] = (char)(0xdc00 + r.Next(2));
                }
                else if (t <= 3)
                {
                    buffer[i] = (char)('a' + r.Next(2));
                }
                else if (4 == t)
                {
                    buffer[i] = (char)(0xe000 + r.Next(2));
                }
            }

            return new string(buffer, 0, end);
        }

        private static string ToHexString(Term t)
        {
            return t.Field + ":" + UnicodeUtil.ToHexString(t.Text());
        }

        private string GetRandomString(Random r)
        {
            string s;
            if (r.Next(5) == 1)
            {
                if (r.Next(3) == 1)
                {
                    s = MakeDifficultRandomUnicodeString(r);
                }
                else
                {
                    s = TestUtil.RandomUnicodeString(r);
                }
            }
            else
            {
                s = TestUtil.RandomRealisticUnicodeString(r);
            }
            return s;
        }

        private sealed class SortTermAsUTF16Comparer : IComparer<Term>
        {
#pragma warning disable 612, 618
            private static readonly IComparer<BytesRef> LegacyComparer = BytesRef.UTF8SortedAsUTF16Comparer;
#pragma warning restore 612, 618

            public int Compare(Term term1, Term term2)
            {
                if (term1.Field.Equals(term2.Field))
                {
                    return LegacyComparer.Compare(term1.Bytes, term2.Bytes);
                }
                else
                {
                    return System.String.Compare(term1.Field, term2.Field, System.StringComparison.Ordinal);
                }
            }
        }

        private static readonly SortTermAsUTF16Comparer TermAsUTF16Comparer = new SortTermAsUTF16Comparer();

        // single straight enum
        private void DoTestStraightEnum(IList<Term> fieldTerms, IndexReader reader, int uniqueTermCount)
        {
            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: top now enum reader=" + reader);
            }
            Fields fields = MultiFields.GetFields(reader);
            {
                // Test straight enum:
                int termCount = 0;
                foreach (string field in fields)
                {
                    Terms terms = fields.GetTerms(field);
                    Assert.IsNotNull(terms);
                    TermsEnum termsEnum = terms.GetIterator(null);
                    BytesRef text;
                    BytesRef lastText = null;
                    while ((text = termsEnum.Next()) != null)
                    {
                        Term exp = fieldTerms[termCount];
                        if (VERBOSE)
                        {
                            Console.WriteLine("  got term=" + field + ":" + UnicodeUtil.ToHexString(text.Utf8ToString()));
                            Console.WriteLine("       exp=" + exp.Field + ":" + UnicodeUtil.ToHexString(exp.Text()));
                            Console.WriteLine();
                        }
                        if (lastText == null)
                        {
                            lastText = BytesRef.DeepCopyOf(text);
                        }
                        else
                        {
                            Assert.IsTrue(lastText.CompareTo(text) < 0);
                            lastText.CopyBytes(text);
                        }
                        Assert.AreEqual(exp.Field, field);
                        Assert.AreEqual(exp.Bytes, text);
                        termCount++;
                    }
                    if (VERBOSE)
                    {
                        Console.WriteLine("  no more terms for field=" + field);
                    }
                }
                Assert.AreEqual(uniqueTermCount, termCount);
            }
        }

        // randomly seeks to term that we know exists, then next's
        // from there
        private void DoTestSeekExists(Random r, IList<Term> fieldTerms, IndexReader reader)
        {
            IDictionary<string, TermsEnum> tes = new Dictionary<string, TermsEnum>();

            // Test random seek to existing term, then enum:
            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: top now seek");
            }

            int num = AtLeast(100);
            for (int iter = 0; iter < num; iter++)
            {
                // pick random field+term
                int spot = r.Next(fieldTerms.Count);
                Term term = fieldTerms[spot];
                string field = term.Field;

                if (VERBOSE)
                {
                    Console.WriteLine("TEST: exist seek field=" + field + " term=" + UnicodeUtil.ToHexString(term.Text()));
                }

                // seek to it
                TermsEnum te;
                if (!tes.TryGetValue(field, out te))
                {
                    te = MultiFields.GetTerms(reader, field).GetIterator(null);
                    tes[field] = te;
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  done get enum");
                }

                // seek should find the term
                Assert.AreEqual(TermsEnum.SeekStatus.FOUND, te.SeekCeil(term.Bytes));

                // now .next() this many times:
                int ct = TestUtil.NextInt(r, 5, 100);
                for (int i = 0; i < ct; i++)
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: now next()");
                    }
                    if (1 + spot + i >= fieldTerms.Count)
                    {
                        break;
                    }
                    term = fieldTerms[1 + spot + i];
                    if (!term.Field.Equals(field))
                    {
                        Assert.IsNull(te.Next());
                        break;
                    }
                    else
                    {
                        BytesRef t = te.Next();

                        if (VERBOSE)
                        {
                            Console.WriteLine("  got term=" + (t == null ? null : UnicodeUtil.ToHexString(t.Utf8ToString())));
                            Console.WriteLine("       exp=" + UnicodeUtil.ToHexString(term.Text().ToString()));
                        }

                        Assert.AreEqual(term.Bytes, t);
                    }
                }
            }
        }

        private void DoTestSeekDoesNotExist(Random r, int numField, IList<Term> fieldTerms, Term[] fieldTermsArray, IndexReader reader)
        {
            IDictionary<string, TermsEnum> tes = new Dictionary<string, TermsEnum>();

            if (VERBOSE)
            {
                Console.WriteLine("TEST: top random seeks");
            }

            {
                int num = AtLeast(100);
                for (int iter = 0; iter < num; iter++)
                {
                    // seek to random spot
                    string field = ("f" + r.Next(numField)).Intern();
                    Term tx = new Term(field, GetRandomString(r));

                    int spot = Array.BinarySearch(fieldTermsArray, tx);

                    if (spot < 0)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: non-exist seek to " + field + ":" + UnicodeUtil.ToHexString(tx.Text()));
                        }

                        // term does not exist:
                        TermsEnum te;
                        if (!tes.TryGetValue(field, out te))
                        {
                            te = MultiFields.GetTerms(reader, field).GetIterator(null);
                            tes[field] = te;
                        }

                        if (VERBOSE)
                        {
                            Console.WriteLine("  got enum");
                        }

                        spot = -spot - 1;

                        if (spot == fieldTerms.Count || !fieldTerms[spot].Field.Equals(field))
                        {
                            Assert.AreEqual(TermsEnum.SeekStatus.END, te.SeekCeil(tx.Bytes));
                        }
                        else
                        {
                            Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, te.SeekCeil(tx.Bytes));

                            if (VERBOSE)
                            {
                                Console.WriteLine("  got term=" + UnicodeUtil.ToHexString(te.Term.Utf8ToString()));
                                Console.WriteLine("  exp term=" + UnicodeUtil.ToHexString(fieldTerms[spot].Text()));
                            }

                            Assert.AreEqual(fieldTerms[spot].Bytes, te.Term);

                            // now .next() this many times:
                            int ct = TestUtil.NextInt(r, 5, 100);
                            for (int i = 0; i < ct; i++)
                            {
                                if (VERBOSE)
                                {
                                    Console.WriteLine("TEST: now next()");
                                }
                                if (1 + spot + i >= fieldTerms.Count)
                                {
                                    break;
                                }
                                Term term = fieldTerms[1 + spot + i];
                                if (!term.Field.Equals(field))
                                {
                                    Assert.IsNull(te.Next());
                                    break;
                                }
                                else
                                {
                                    BytesRef t = te.Next();

                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("  got term=" + (t == null ? null : UnicodeUtil.ToHexString(t.Utf8ToString())));
                                        Console.WriteLine("       exp=" + UnicodeUtil.ToHexString(term.Text().ToString()));
                                    }

                                    Assert.AreEqual(term.Bytes, t);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestSurrogatesOrder()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                .SetCodec(new PreFlexRWCodec()));

            int numField = TestUtil.NextInt(Random(), 2, 5);

            int uniqueTermCount = 0;

            int tc = 0;

            var fieldTerms = new List<Term>();

            for (int f = 0; f < numField; f++)
            {
                string field = "f" + f;
                int numTerms = AtLeast(200);

                ISet<string> uniqueTerms = new HashSet<string>();

                for (int i = 0; i < numTerms; i++)
                {
                    string term = GetRandomString(Random()) + "_ " + (tc++);
                    uniqueTerms.Add(term);
                    fieldTerms.Add(new Term(field, term));
                    Documents.Document doc = new Documents.Document();
                    doc.Add(NewStringField(field, term, Field.Store.NO));
                    w.AddDocument(doc);
                }
                uniqueTermCount += uniqueTerms.Count;
            }

            IndexReader reader = w.Reader;

            if (VERBOSE)
            {
                fieldTerms.Sort(TermAsUTF16Comparer);

                Console.WriteLine("\nTEST: UTF16 order");
                foreach (Term t in fieldTerms)
                {
                    Console.WriteLine("  " + ToHexString(t));
                }
            }

            // sorts in code point order:
            fieldTerms.Sort();

            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: codepoint order");
                foreach (Term t in fieldTerms)
                {
                    Console.WriteLine("  " + ToHexString(t));
                }
            }

            Term[] fieldTermsArray = fieldTerms.ToArray();

            //SegmentInfo si = makePreFlexSegment(r, "_0", dir, fieldInfos, codec, fieldTerms);

            //FieldsProducer fields = codec.fieldsProducer(new SegmentReadState(dir, si, fieldInfos, 1024, 1));
            //Assert.IsNotNull(fields);

            DoTestStraightEnum(fieldTerms, reader, uniqueTermCount);
            DoTestSeekExists(Random(), fieldTerms, reader);
            DoTestSeekDoesNotExist(Random(), numField, fieldTerms, fieldTermsArray, reader);

            reader.Dispose();
            w.Dispose();
            dir.Dispose();
        }
    }
}