using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace Patcher.Tests.Unit
{
    public class SimplePatcherTests
    {
        [Fact]
        public void Test1()
        {
            var sourceObj = new
            {
                FirstName = "Jon",
                LastName = "Sagara"
            };

            dynamic sourceDyn = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceObj));

            var destination = new EmployeeNoMiddleName { FirstName = "Steve", LastName = "Stevenson" };

            SimplePatcher.PatchFromJObject<EmployeeNoMiddleName>(sourceDyn, destination);

            destination.FirstName.ShouldBe(sourceObj.FirstName);
            destination.LastName.ShouldBe(sourceObj.LastName);
        }
    }

    public class EmployeeNoMiddleName
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
