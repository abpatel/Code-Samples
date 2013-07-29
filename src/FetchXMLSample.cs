using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SampleCode
{
    class Program
    {

       
        private static void Tokenize()
        {
            List<KeyValuePair<string, string>> kvps = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("id","1234"),
                new KeyValuePair<string,string>("name","TEST-123"),
            };
            string xml = @"<fetch distinct='false' mapping='logical' aggregate='true'> 
                            <entity name='opportunity'> 
                               <attribute name='name' alias='opportunity_colcount' aggregate='countcolumn'/> 
                               <filter type='and'>
                                        <condition attribute='id' operator='eq' value='token1' />
                               </filter>
                                <filter type='and'>
                                        <condition attribute='name' operator='eq' value='token2' />
                               </filter>
                            </entity> 
                        </fetch>";
            XElement ele = XElement.Parse(xml);
            //var elements = ele.Descendants().Where(e => e.Attributes().Any(a => a.Name == "attribute"));
            //foreach (var element in elements)
            //{
            //    var attr = element.Attributes().SingleOrDefault(x => x.Name == "value");
            //    if (attr != null)
            //    {
            //        attr.Value = "test";
            //    }
            //}
            //var attributes = ele.Descendants()
            //    .Where(e => e.Attributes()
            //                .SingleOrDefault(a => a.Name == "attribute" && 
            //                            !string.IsNullOrEmpty(a.Value)
            //                       ) != null
            //           )
            //    .Select(e => e.Attributes());
            var attributes = ele.Descendants()
                .Attributes()
                .Where(a => a.Name == "attribute" && 
                                        !string.IsNullOrEmpty(a.Value));

            var query = from a in attributes
                    join k in kvps
                        on a.Value equals k.Key
                    select new { Attribute = a, Value = k.Value,  };
            foreach (var item in query)
            {
                item.Attribute.Parent.SetAttributeValue("value",item.Value);
            }
        }

        static void Main(string[] args)
        {
            Tokenize();
        }
    }
}
