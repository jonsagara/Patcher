using System;
using System.Collections.Generic;
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
            SimplePatcher.PatchFromJObject(sourceDyn, destination);

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
                SimplePatcher.PatchFromJObject(sourceDyn, destination);
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

            SimplePatcher.PatchFromJObject(sourceDyn, destination, ignoreUnknownProperties: true);
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

            SimplePatcher.PatchFromJObject(sourceDyn, destination);
            destination.FirstName.ShouldBe(sourceObj.FirstName);
            destination.MiddleName.ShouldBe(sourceObj.MiddleName);
            destination.LastName.ShouldBe(sourceObj.LastName);
            destination.DateOfBirth.ShouldBe(dob);
            destination.Dependents.ShouldBe(dependents);
        }

        [Fact]
        public void TryingToSetDestinationIEnumerable_ThrowsNotSupportedException()
        {
            var sourceObj = new
            {
                FirstName = "Tommy",
                MiddleName = "Hank",
                LastName = "Tomorrow",
                Children = new []
                {
                    "Dan",
                    "Marissa",
                    "Mark"
                }
            };

            var dob = DateTime.Parse("1980-01-01");
            var dependents = 3;

            dynamic sourceDyn = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceObj));
            var destination = new FullEmployee { FirstName = "Steve", LastName = "Stevenson", MiddleName = "Aero", DateOfBirth = dob, Dependents = dependents };

            Should.Throw<NotSupportedException>(() =>
            {
                SimplePatcher.PatchFromJObject(sourceDyn, destination);
            });
        }

        [Fact]
        public void TryingToSetDestinationComplexType_ThrowsNotSupportedException()
        {
            var sourceObj = new
            {
                FirstName = "Tommy",
                MiddleName = "Hank",
                LastName = "Tomorrow",
                Spouse = new
                {
                    FirstName = "Tammy",
                    LastName = "Tomorrow"
                }
            };

            var dob = DateTime.Parse("1980-01-01");
            var dependents = 3;

            dynamic sourceDyn = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(sourceObj));
            var destination = new FullEmployee { FirstName = "Steve", LastName = "Stevenson", MiddleName = "Aero", DateOfBirth = dob, Dependents = dependents, Spouse = new Person { FirstName = "Sally", LastName = "Stevenson" } };

            Should.Throw<NotSupportedException>(() =>
            {
                SimplePatcher.PatchFromJObject(sourceDyn, destination);
            });
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

        public List<string> Children { get; private set; } = new List<string>();

        public Person Spouse { get; set; }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
