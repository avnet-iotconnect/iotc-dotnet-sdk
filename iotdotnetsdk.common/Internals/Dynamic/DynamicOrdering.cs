using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace iotdotnetsdk.common.Internals.Dynamic
{
    internal class DynamicOrdering
    {
        public Expression Selector;
        public bool Ascending;
    }
}
