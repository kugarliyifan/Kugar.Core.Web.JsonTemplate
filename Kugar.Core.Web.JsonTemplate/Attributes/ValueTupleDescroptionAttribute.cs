using System;

namespace Kugar.Core.Web.JsonTemplate.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter,AllowMultiple = true)]
    public class ValueTupleDescroptionAttribute : Attribute
    {
        public ValueTupleDescroptionAttribute(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }

        public string Name { set; get; }

        public string Description { set; get; }
    }
}
