﻿using Lucene.Net.Attributes;
using Lucene.Net.Index;
using NUnit.Framework;

namespace Lucene.Net.Codecs.SimpleText
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

    public class TestSimpleTextStoredFieldsFormat : BaseStoredFieldsFormatTestCase
    {

        protected override Codec Codec
        {
            get
            {
                return new SimpleTextCodec();
            }
        }

        #region BaseStoredFieldsFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestRandomStoredFields()
        {
            base.TestRandomStoredFields();
        }

        [Test]
        // LUCENE-1727: make sure doc fields are stored in order
        public override void TestStoredFieldsOrder()
        {
            base.TestStoredFieldsOrder();
        }

        [Test]
        // LUCENE-1219
        public override void TestBinaryFieldOffsetLength()
        {
            base.TestBinaryFieldOffsetLength();
        }

        [Test]
        public override void TestNumericField()
        {
            base.TestNumericField();
        }

        [Test]
        public override void TestIndexedBit()
        {
            base.TestIndexedBit();
        }

        [Test]
        public override void TestReadSkip()
        {
            base.TestReadSkip();
        }

        [Test]
        public override void TestEmptyDocs()
        {
            base.TestEmptyDocs();
        }

        [Test]
        public override void TestConcurrentReads()
        {
            base.TestConcurrentReads();
        }

        [Test]
        public override void TestWriteReadMerge()
        {
            base.TestWriteReadMerge();
        }

        [Test, LongRunningTest]
        public override void TestBigDocuments()
        {
            base.TestBigDocuments();
        }

        [Test]
        public override void TestBulkMergeWithDeletes()
        {
            base.TestBulkMergeWithDeletes();
        }

        #endregion

        #region BaseIndexFileFormatTestCase
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestMergeStability()
        {
            base.TestMergeStability();
        }

        #endregion
    }
}