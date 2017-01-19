using System;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Patcher.Tests.Unit
{
    public class SimplePatcherTests
    {
        [Fact]
        public void MatchingSourceAndDestinationProperties_Successful()
        {
            var sourceObj = new
            {
                FirstName = "Tommy",
                LastName = "Tomorrow"
            };

            dynamic sourceDyn = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceObj));

            var destination = new EmployeeNoMiddleName { FirstName = "Steve", LastName = "Stevenson" };
            SimplePatcher.PatchFromJObject<EmployeeNoMiddleName>(sourceDyn, destination);

            destination.FirstName.ShouldBe(sourceObj.FirstName);
            destination.LastName.ShouldBe(sourceObj.LastName);
        }

        [Fact]
        public void UnknownSourceProperty_ThrowsInvalidOperationException()
        {
            var sourceObj = new
            {
                FirstName = "Tommy",
                MiddleName = "Hank",
                LastName = "Tomorrow"
            };

            dynamic sourceDyn = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceObj));
            var destination = new EmployeeNoMiddleName { FirstName = "Steve", LastName = "Stevenson" };

            Should.Throw<InvalidOperationException>(() =>
            {
                SimplePatcher.PatchFromJObject<EmployeeNoMiddleName>(sourceDyn, destination);
            });
        }

        [Fact]
        public void UnknownSourceProperty_IgnoreUnknownSourceProperties_Successful()
        {
            var sourceObj = new
            {
                FirstName = "Tommy",
                MiddleName = "Hank",
                LastName = "Tomorrow"
            };

            dynamic sourceDyn = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceObj));
            var destination = new EmployeeNoMiddleName { FirstName = "Steve", LastName = "Stevenson" };

            SimplePatcher.PatchFromJObject<EmployeeNoMiddleName>(sourceDyn, destination, ignoreUnknownProperties: true);
            destination.FirstName.ShouldBe(sourceObj.FirstName);
            destination.LastName.ShouldBe(sourceObj.LastName);
        }

        [Fact]
        public void UnspecifiedDestinationProperties_NotModified_Successful()
        {
            var sourceObj = new
            {
                FirstName = "Tommy",
                MiddleName = "Hank",
                LastName = "Tomorrow"
            };

            var dob = DateTime.Parse("1980-01-01");
            var dependents = 3;

            dynamic sourceDyn = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceObj));
            var destination = new FullEmployee { FirstName = "Steve", LastName = "Stevenson", MiddleName = "Aero", DateOfBirth = dob, Dependents = dependents };

            SimplePatcher.PatchFromJObject<EmployeeNoMiddleName>(sourceDyn, destination);
            destination.FirstName.ShouldBe(sourceObj.FirstName);
            destination.MiddleName.ShouldBe(sourceObj.MiddleName);
            destination.LastName.ShouldBe(sourceObj.LastName);
            destination.DateOfBirth.ShouldBe(dob);
            destination.Dependents.ShouldBe(dependents);
        }
    }

    public class EmployeeNoMiddleName
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class FullEmployee : EmployeeNoMiddleName
    {
        public string MiddleName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Dependents { get; set; }
    }
}
