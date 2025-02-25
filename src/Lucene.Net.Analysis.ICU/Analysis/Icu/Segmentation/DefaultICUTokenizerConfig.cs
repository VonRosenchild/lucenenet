﻿// Lucene version compatibility level 7.1.0
using ICU4N.Globalization;
using ICU4N.Text;
using ICU4N.Util;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Support;
using System;
using System.IO;
using System.Reflection;

namespace Lucene.Net.Analysis.Icu.Segmentation
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

    /// <summary>
    /// Default <see cref="ICUTokenizerConfig"/> that is generally applicable
    /// to many languages.
    /// </summary>
    /// <remarks>
    /// Generally tokenizes Unicode text according to UAX#29 
    /// (<see cref="T:BreakIterator.GetWordInstance(ULocale.ROOT)"/>), 
    /// but with the following tailorings:
    /// <list type="bullet">
    ///     <item><description>Thai, Lao, Myanmar, Khmer, and CJK text is broken into words with a dictionary.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </remarks>
    public class DefaultICUTokenizerConfig : ICUTokenizerConfig
    {
        /// <summary>Token type for words containing ideographic characters</summary>
        public static readonly string WORD_IDEO = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.IDEOGRAPHIC];
        /// <summary>Token type for words containing Japanese hiragana</summary>
        public static readonly string WORD_HIRAGANA = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HIRAGANA];
        /// <summary>Token type for words containing Japanese katakana</summary>
        public static readonly string WORD_KATAKANA = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.KATAKANA];
        /// <summary>Token type for words containing Korean hangul</summary>
        public static readonly string WORD_HANGUL = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HANGUL];
        /// <summary>Token type for words that contain letters</summary>
        public static readonly string WORD_LETTER = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.ALPHANUM];
        /// <summary>Token type for words that appear to be numbers</summary>
        public static readonly string WORD_NUMBER = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.NUM];

        /// <summary>
        /// the default breakiterators in use. these can be expensive to
        /// instantiate, cheap to clone.
        /// </summary>
        // we keep the cjk breaking separate, thats because it cannot be customized (because dictionary
        // is only triggered when kind = WORD, but kind = LINE by default and we have no non-evil way to change it)
        private static readonly BreakIterator cjkBreakIterator = BreakIterator.GetWordInstance(ULocale.ROOT);

        // TODO: if the wrong version of the ICU jar is used, loading these data files may give a strange error.
        // maybe add an explicit check? http://icu-project.org/apiref/icu4j/com/ibm/icu/util/VersionInfo.html

        // the same as ROOT, except no dictionary segmentation for cjk
        private static readonly BreakIterator defaultBreakIterator =
            ReadBreakIterator("Default.brk");
        private static readonly BreakIterator myanmarSyllableIterator =
            ReadBreakIterator("MyanmarSyllable.brk");

        // TODO: deprecate this boolean? you only care if you are doing super-expert stuff...
        private readonly bool cjkAsWords;
        private readonly bool myanmarAsWords;

        /// <summary>
        /// Creates a new config. This object is lightweight, but the first
        /// time the class is referenced, breakiterators will be initialized.
        /// </summary>
        /// <param name="cjkAsWords">true if cjk text should undergo dictionary-based segmentation,
        /// otherwise text will be segmented according to UAX#29 defaults.</param>
        /// <param name="myanmarAsWords">If this is true, all Han+Hiragana+Katakana words will be tagged as IDEOGRAPHIC.</param>
        public DefaultICUTokenizerConfig(bool cjkAsWords, bool myanmarAsWords)
        {
            this.cjkAsWords = cjkAsWords;
            this.myanmarAsWords = myanmarAsWords;
        }

        public override bool CombineCJ
        {
            get { return cjkAsWords; }
        }

        public override BreakIterator GetBreakIterator(int script)
        {
            switch (script)
            {
                case UScript.Japanese: return (BreakIterator)cjkBreakIterator.Clone();
                case UScript.Myanmar:
                    if (myanmarAsWords)
                    {
                        return (BreakIterator)defaultBreakIterator.Clone();
                    }
                    else
                    {
                        return (BreakIterator)myanmarSyllableIterator.Clone();
                    }
                default: return (BreakIterator)defaultBreakIterator.Clone();
            }
        }

        public override string GetType(int script, RuleStatus ruleStatus)
        {
            switch (ruleStatus)
            {
                case RuleStatus.WordIdeo:
                    return WORD_IDEO;
                case RuleStatus.WordKana: //RuleBasedBreakIterator.WORD_KANA:
                    return script == UScript.Hiragana ? WORD_HIRAGANA : WORD_KATAKANA;
                case RuleStatus.WordLetter: //RuleBasedBreakIterator.WORD_LETTER:
                    return script == UScript.Hangul ? WORD_HANGUL : WORD_LETTER;
                case RuleStatus.WordNumber: //RuleBasedBreakIterator.WORD_NUMBER:
                    return WORD_NUMBER;
                default: /* some other custom code */
                    return "<OTHER>";
            }
        }

        private static RuleBasedBreakIterator ReadBreakIterator(string filename)
        {
            using (Stream @is =
              typeof(DefaultICUTokenizerConfig).GetTypeInfo().Assembly.FindAndGetManifestResourceStream(typeof(DefaultICUTokenizerConfig), filename))
            {
                try
                {
                    RuleBasedBreakIterator bi =
                        RuleBasedBreakIterator.GetInstanceFromCompiledRules(@is);
                    return bi;
                }
                catch (IOException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }
        }
    }
}
