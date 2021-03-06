﻿using System.Collections;
using System.Collections.Generic;
using System.Linq.Dynamic.Core.Exceptions;
using System.Linq.Dynamic.Core.Tests.Helpers.Models;
using Linq.PropertyTranslator.Core;
using QueryInterceptor.Core;
using Xunit;
using NFluent;
#if EFCORE
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#else
using Microsoft.AspNet.Identity.EntityFramework;
#endif

namespace System.Linq.Dynamic.Core.Tests
{
    public partial class QueryableTests
    {
        /// <summary>
        /// Cannot work with property which in base class. https://github.com/StefH/System.Linq.Dynamic.Core/issues/23
        /// </summary>
        [Fact]
        public void Select_Dynamic_PropertyInBaseClass()
        {
            var queryable = new[] { new IdentityUser("a"), new IdentityUser("b") }.AsQueryable();

            var expected = queryable.Select(i => i.Id);
            var dynamic = queryable.Select<string>("Id");

            Assert.Equal(expected.ToArray(), dynamic.ToArray());
        }

        [Fact]
        public void Select_Dynamic1()
        {
            // Assign
            var qry = User.GenerateSampleModels(1).AsQueryable();

            // Act
            var userRoles1 = qry.Select("new (Roles.Select(Id) as RoleIds)");
            var userRoles2 = qry.Select("new (Roles.Select(it.Id) as RoleIds)");

            // Assert
            Check.That(userRoles1.Count()).IsEqualTo(1);
            Check.That(userRoles2.Count()).IsEqualTo(1);
        }

        [Fact]
        public void Select_Dynamic2()
        {
            // Assign
            var qry = User.GenerateSampleModels(1).AsQueryable();

            // Act
            var userRoles = qry.Select("new (Roles.Select(it).ToArray() as Rolez)");

            // Assert
            Check.That(userRoles.Count()).IsEqualTo(1);
        }

        [Fact]
        public void Select_Dynamic3()
        {
            //Arrange
            List<int> range = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var testList = User.GenerateSampleModels(100);
            var qry = testList.AsQueryable();

            //Act
            IEnumerable rangeResult = range.AsQueryable().Select("it * it");
            var userNames = qry.Select("UserName");
            var userFirstName = qry.Select("new (UserName, Profile.FirstName as MyFirstName)");
            var userRoles = qry.Select("new (UserName, Roles.Select(Id) AS RoleIds)");

            //Assert
            Assert.Equal(range.Select(x => x * x).ToArray(), rangeResult.Cast<int>().ToArray());

#if NET35
            Assert.Equal(testList.Select(x => x.UserName).ToArray(), userNames.AsEnumerable().Cast<string>().ToArray());
            Assert.Equal(
                testList.Select(x => "{ UserName = " + x.UserName + ", MyFirstName = " + x.Profile.FirstName + " }").ToArray(),
                userFirstName.Cast<object>().Select(x => x.ToString()).ToArray());
            Assert.Equal(testList[0].Roles.Select(x => x.Id).ToArray(), Enumerable.ToArray(userRoles.First().GetDynamicProperty<IEnumerable<Guid>>("RoleIds")));
#else
            Assert.Equal(testList.Select(x => x.UserName).ToArray(), userNames.Cast<string>().ToArray());
            Assert.Equal(
                testList.Select(x => "{ UserName = " + x.UserName + ", MyFirstName = " + x.Profile.FirstName + " }").ToArray(),
                userFirstName.AsEnumerable().Select(x => x.ToString()).Cast<string>().ToArray());
            Assert.Equal(testList[0].Roles.Select(x => x.Id).ToArray(), Enumerable.ToArray(userRoles.First().RoleIds));
#endif
        }

        [Fact]
        public void Select_Dynamic_Add_Integers()
        {
            // Arrange
            var range = new List<int> { 1, 2 };

            // Act
            IEnumerable rangeResult = range.AsQueryable().Select("it + 1");

            // Assert
            Assert.Equal(range.Select(x => x + 1).ToArray(), rangeResult.Cast<int>().ToArray());
        }

        [Fact]
        public void Select_Dynamic_Add_Strings()
        {
            // Arrange
            var range = new List<string> { "a", "b" };

            // Act
            IEnumerable rangeResult = range.AsQueryable().Select("it + \"c\"");

            // Assert
            Assert.Equal(range.Select(x => x + "c").ToArray(), rangeResult.Cast<string>().ToArray());
        }

        [Fact]
        public void Select_Dynamic_WithIncludes()
        {
            // Arrange
            var qry = new List<Entities.Employee>().AsQueryable();

            // Act
            string includesX =
                ", it.Company as TEntity__Company, it.Company.MainCompany as TEntity__Company_MainCompany, it.Country as TEntity__Country, it.Function as TEntity__Function, it.SubFunction as TEntity__SubFunction";
            string select = $"new (\"__Key__\" as __Key__, it AS TEntity__{includesX})";

            var userNames = qry.Select(select).ToDynamicList();

            // Assert
            Assert.NotNull(userNames);
        }

        [Fact]
        public void Select_Dynamic_WithPropertyVisitorAndQueryInterceptor()
        {
            var testList = new List<Entities.Employee>
            {
                new Entities.Employee {FirstName = "first", LastName = "last"}
            };
            var qry = testList.AsEnumerable().AsQueryable().InterceptWith(new PropertyVisitor());

            var dynamicSelect = qry.Select("new (FirstName, LastName, FullName)").ToDynamicList();
            Assert.NotNull(dynamicSelect);
            Assert.Single(dynamicSelect);

            var firstEmployee = dynamicSelect.FirstOrDefault();
            Assert.NotNull(firstEmployee);

            Assert.Equal("first last", firstEmployee.FullName);
        }

        [Fact]
        public void Select_Dynamic_TResult()
        {
            //Arrange
            List<int> range = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var testList = User.GenerateSampleModels(100);
            var qry = testList.AsQueryable();

            //Act
            IEnumerable rangeResult = range.AsQueryable().Select<int>("it * it").ToList();
            var userNames = qry.Select<string>("UserName").ToList();
            var userProfiles = qry.Select<UserProfile>("Profile").ToList();

            //Assert
            Assert.Equal(range.Select(x => x * x).ToList(), rangeResult);
            Assert.Equal(testList.Select(x => x.UserName).ToList(), userNames);
            Assert.Equal(testList.Select(x => x.Profile).ToList(), userProfiles);
        }

        [Fact]
        public void Select_Dynamic_IntoType()
        {
            //Arrange
            List<int> range = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var testList = User.GenerateSampleModels(10);
            var qry = testList.AsQueryable();

            //Act
            IEnumerable rangeResult = range.AsQueryable().Select(typeof(int), "it * it");
            var userNames = qry.Select(typeof(string), "UserName");
            var userProfiles = qry.Select(typeof(UserProfile), "Profile");

            //Assert
            Assert.Equal(range.Select(x => x * x).Cast<object>().ToList(), rangeResult.ToDynamicList());
            Assert.Equal(testList.Select(x => x.UserName).Cast<object>().ToList(), userNames.ToDynamicList());
            Assert.Equal(testList.Select(x => x.Profile).Cast<object>().ToList(), userProfiles.ToDynamicList());
        }

        public class Example
        {
            public DateTime Time { get; set; }
            public DayOfWeek? DOWNull { get; set; }
            public DayOfWeek DOW { get; set; }
            public int Sec { get; set; }
            public int? SecNull { get; set; }
        }

        public class ExampleWithConstructor
        {
            public DateTime Time { get; set; }
            public DayOfWeek? DOWNull { get; set; }
            public DayOfWeek DOW { get; set; }
            public int Sec { get; set; }
            public int? SecNull { get; set; }

            public ExampleWithConstructor(DateTime t, DayOfWeek? dn, DayOfWeek d, int s, int? sn)
            {
                Time = t;
                DOWNull = dn;
                DOW = d;
                Sec = s;
                SecNull = sn;
            }
        }

        [Fact]
        public void Select_Dynamic_IntoTypeWithNullableProperties1()
        {
            // Arrange
            var dates = Enumerable.Repeat(0, 7)
                    .Select((d, i) => new DateTime(2000, 1, 1).AddDays(i).AddSeconds(i))
                    .AsQueryable();

            // Act
            IQueryable<Example> result = dates
                .Select(d => new Example { Time = d, DOWNull = d.DayOfWeek, DOW = d.DayOfWeek, Sec = d.Second, SecNull = d.Second });
            IQueryable<Example> resultDynamic = dates
                .Select<Example>("new (it as Time, DayOfWeek as DOWNull, DayOfWeek as DOW, Second as Sec, int?(Second) as SecNull)");

            // Assert
            Check.That(resultDynamic.First()).Equals(result.First());
            Check.That(resultDynamic.Last()).Equals(result.Last());
        }

        [Fact]
        public void Select_Dynamic_IntoTypeWithNullableProperties2()
        {
            // Arrange
            var dates = Enumerable.Repeat(0, 7)
                    .Select((d, i) => new DateTime(2000, 1, 1).AddDays(i).AddSeconds(i))
                    .AsQueryable();

            // Act
            IQueryable<ExampleWithConstructor> result = dates
                .Select(d => new ExampleWithConstructor(d, d.DayOfWeek, d.DayOfWeek, d.Second, d.Second));
            IQueryable<ExampleWithConstructor> resultDynamic = dates
                .Select<ExampleWithConstructor>("new (it as Time, DayOfWeek as DOWNull, DayOfWeek as DOW, Second as Sec, int?(Second) as SecNull)");

            // Assert
            Check.That(resultDynamic.First()).Equals(result.First());
            Check.That(resultDynamic.Last()).Equals(result.Last());
        }

        [Fact]
        public void Select_Dynamic_Exceptions()
        {
            //Arrange
            var testList = User.GenerateSampleModels(100, allowNullableProfiles: true);
            var qry = testList.AsQueryable();

            //Act
            Assert.Throws<ParseException>(() => qry.Select("Bad"));
            Assert.Throws<ParseException>(() => qry.Select("Id, UserName"));
            Assert.Throws<ParseException>(() => qry.Select("new Id, UserName"));
            Assert.Throws<ParseException>(() => qry.Select("new (Id, UserName"));
            Assert.Throws<ParseException>(() => qry.Select("new (Id, UserName, Bad)"));

            Assert.Throws<ArgumentNullException>(() => DynamicQueryableExtensions.Select(null, "Id"));
            Assert.Throws<ArgumentNullException>(() => qry.Select(null));
            Assert.Throws<ArgumentException>(() => qry.Select(""));
            Assert.Throws<ArgumentException>(() => qry.Select(" "));
        }
    }
}
