﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// TODO Sort CssPropertyValues when compressing?
namespace Alba.CsCss.Style
{
    [DebuggerDisplay ("{DebugDisplayCount,nq}")]
    public class CssDeclaration
    {
        private readonly List<CssProperty> mOrder = new List<CssProperty>();
        private nsCSSCompressedDataBlock mData, mImportantData;
        private bool mImmutable;

        internal void ValueAppended (CssProperty aProperty)
        {
            mOrder.Remove(aProperty);
            mOrder.Add(aProperty);
        }

        internal void CompressFrom (nsCSSExpandedDataBlock aExpandedData)
        {
            aExpandedData.Compress(out mData, out mImportantData);
            aExpandedData.AssertInitialState();
        }

        internal void ExpandTo (nsCSSExpandedDataBlock aExpandedData)
        {
            AssertMutable();
            aExpandedData.AssertInitialState();
            aExpandedData.Expand(mData, mImportantData);
        }

        internal bool TryReplaceValue (CssProperty aProperty, bool aIsImportant, nsCSSExpandedDataBlock aFromBlock, ref bool aChanged)
        {
            AssertMutable();
            Debug.Assert(mData != null, "called while expanded");

            if (nsCSSProps.IsShorthand(aProperty)) {
                aChanged = false;
                return false;
            }
            nsCSSCompressedDataBlock block = aIsImportant ? mImportantData : mData;
            if (block == null) {
                aChanged = false;
                return false;
            }
            return block.TryReplaceValue(aProperty, aFromBlock, ref aChanged);
        }

        [Conditional ("DEBUG")]
        internal void AssertMutable ()
        {
            Debug.Assert(IsMutable(), "someone forgot to call EnsureMutable");
        }

        internal bool IsMutable ()
        {
            return !mImmutable;
        }

        internal void SetImmutable ()
        {
            mImmutable = true;
        }

        internal void ClearData ()
        {
            AssertMutable();
            mData = null;
            mImportantData = null;
            mOrder.Clear();
        }

        // My

        internal void Fix ()
        {
            FixData(mData);
            FixData(mImportantData);
        }

        internal void FixData (nsCSSCompressedDataBlock aDataBlock)
        {
            mData.mData = OrderDataByOrder(mData.mData).ToArray();
            if (mImportantData != null)
                mImportantData.mData = OrderDataByOrder(mImportantData.mData).ToArray();
            mOrder.Clear();
        }

        private IEnumerable<CssPropertyValue> OrderDataByOrder (IEnumerable<CssPropertyValue> data)
        {
            return mOrder.Join(data, p => p, pv => pv.mProperty, (p, pv) => pv);
        }

        // Public interface

        public IReadOnlyList<CssPropertyValue> Data
        {
            get { return mData.mData; }
        }

        public IReadOnlyList<CssPropertyValue> ImportantData
        {
            get { return mImportantData.mData; }
        }

        public CssValue GetValue (CssProperty prop)
        {
            return mData.ValueFor(prop);
        }

        public CssValue GetImportantValue (CssProperty prop)
        {
            return mData.ValueFor(prop);
        }

        internal string DebugDisplayCount
        {
            get
            {
                return mImportantData != null
                    ? string.Format("Count = {0} + {1}", mData.mData.Length, mImportantData.mData.Length)
                    : string.Format("Count = {0}", mData.mData.Length);
            }
        }
    }
}