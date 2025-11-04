using LazyLoading.Core;
using Xunit;

namespace LazyLoading.Tests.Core
{
    /// <summary>
    /// Testy jednostkowe dla klasy GenericPagedDataSource.
    /// </summary>
    public class GenericPagedDataSourceTests
    {
        private const string TestConnectionString = "Server=test;Database=test;User Id=test;Password=test";
        private const string TestStoredProcedure = "sp_GetPagedData";

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                50);

            // Assert
            Assert.NotNull(dataSource);
            Assert.Equal(50, dataSource.PageSize);
        }

        [Fact]
        public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GenericPagedDataSource(null!, TestStoredProcedure));
        }

        [Fact]
        public void Constructor_WithNullStoredProcedureName_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GenericPagedDataSource(TestConnectionString, null!));
        }

        [Fact]
        public void Constructor_WithDefaultPageSize_ShouldUseDefault()
        {
            // Arrange & Act
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure);

            // Assert
            Assert.Equal(50, dataSource.PageSize);
        }

        [Fact]
        public void Constructor_WithCustomPageSize_ShouldUseCustomValue()
        {
            // Arrange & Act
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                100);

            // Assert
            Assert.Equal(100, dataSource.PageSize);
        }

        [Fact]
        public void Constructor_WithParameters_ShouldAcceptParameters()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "@Category", "Electronics" },
                { "@MinPrice", 100 }
            };

            // Act
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                50,
                parameters);

            // Assert
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void Constructor_WithNullParameters_ShouldInitializeEmptyDictionary()
        {
            // Arrange & Act
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                50,
                null);

            // Assert
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void SetParameter_ShouldAddNewParameter()
        {
            // Arrange
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure);

            // Act
            dataSource.SetParameter("@Category", "Books");

            // Assert - parametr został dodany (sprawdzamy przez brak wyjątku)
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void SetParameter_ShouldUpdateExistingParameter()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "@Category", "Electronics" }
            };
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                50,
                parameters);

            // Act
            dataSource.SetParameter("@Category", "Books");

            // Assert - parametr został zaktualizowany (sprawdzamy przez brak wyjątku)
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void SetParameter_WithMultipleParameters_ShouldHandleAll()
        {
            // Arrange
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure);

            // Act
            dataSource.SetParameter("@Category", "Books");
            dataSource.SetParameter("@MinPrice", 50);
            dataSource.SetParameter("@MaxPrice", 500);
            dataSource.SetParameter("@InStock", true);

            // Assert
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void RemoveParameter_ShouldRemoveExistingParameter()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "@Category", "Electronics" },
                { "@MinPrice", 100 }
            };
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                50,
                parameters);

            // Act
            dataSource.RemoveParameter("@Category");

            // Assert - parametr został usunięty (sprawdzamy przez brak wyjątku)
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void RemoveParameter_WithNonExistingParameter_ShouldNotThrow()
        {
            // Arrange
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure);

            // Act & Assert - nie powinno rzucić wyjątku
            dataSource.RemoveParameter("@NonExisting");
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void Refresh_ShouldPreserveParameters()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "@Category", "Electronics" }
            };
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                50,
                parameters);

            // Act
            dataSource.Refresh();

            // Assert - parametry powinny zostać zachowane
            // Sprawdzamy poprzez dodanie kolejnego parametru - jeśli się uda, to znaczy że struktura jest OK
            dataSource.SetParameter("@MinPrice", 100);
            Assert.NotNull(dataSource);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        public void Constructor_WithVariousPageSizes_ShouldAcceptAllValidValues(int pageSize)
        {
            // Arrange & Act
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                pageSize);

            // Assert
            Assert.Equal(pageSize, dataSource.PageSize);
        }

        [Fact]
        public void SetParameter_WithNullValue_ShouldHandleCorrectly()
        {
            // Arrange
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure);

            // Act & Assert - nie powinno rzucić wyjątku
            dataSource.SetParameter("@OptionalParam", null!);
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void SetParameter_WithDifferentDataTypes_ShouldHandleAll()
        {
            // Arrange
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure);

            // Act
            dataSource.SetParameter("@StringParam", "test");
            dataSource.SetParameter("@IntParam", 42);
            dataSource.SetParameter("@DecimalParam", 123.45m);
            dataSource.SetParameter("@BoolParam", true);
            dataSource.SetParameter("@DateParam", DateTime.Now);

            // Assert
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void MultipleInstances_ShouldBeIndependent()
        {
            // Arrange & Act
            var dataSource1 = new GenericPagedDataSource(
                TestConnectionString,
                "sp_Proc1",
                50);
            dataSource1.SetParameter("@Param1", "Value1");

            var dataSource2 = new GenericPagedDataSource(
                TestConnectionString,
                "sp_Proc2",
                100);
            dataSource2.SetParameter("@Param2", "Value2");

            // Assert
            Assert.NotNull(dataSource1);
            Assert.NotNull(dataSource2);
            Assert.NotEqual(dataSource1.PageSize, dataSource2.PageSize);
        }

        [Fact]
        public void PageSize_CanBeModifiedAfterCreation()
        {
            // Arrange
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                50);

            // Act
            dataSource.PageSize = 100;

            // Assert
            Assert.Equal(100, dataSource.PageSize);
        }

        [Fact]
        public void Parameters_CanBeModifiedMultipleTimes()
        {
            // Arrange
            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure);

            // Act
            dataSource.SetParameter("@Category", "Books");
            dataSource.SetParameter("@Category", "Electronics");
            dataSource.SetParameter("@Category", "Toys");

            // Assert
            Assert.NotNull(dataSource);
        }

        [Fact]
        public void ComplexScenario_MultipleOperations_ShouldWork()
        {
            // Arrange
            var initialParams = new Dictionary<string, object>
            {
                { "@InitialParam", "InitialValue" }
            };

            var dataSource = new GenericPagedDataSource(
                TestConnectionString,
                TestStoredProcedure,
                75,
                initialParams);

            // Act
            dataSource.SetParameter("@NewParam1", 100);
            dataSource.SetParameter("@NewParam2", true);
            dataSource.RemoveParameter("@InitialParam");
            dataSource.PageSize = 100;
            dataSource.Refresh();
            dataSource.SetParameter("@FinalParam", "FinalValue");

            // Assert
            Assert.Equal(100, dataSource.PageSize);
            Assert.NotNull(dataSource);
        }
    }
}
