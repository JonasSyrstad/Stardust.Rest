using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Stardust.Interstellar.Rest.JsonPatch;
using System.IO;
using Xunit;

namespace MiscTests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var serializer = JsonSerializer.Create();
            long length;
            using (var memStream = new MemoryStream())
            {
                using (var stream = new StreamWriter(memStream))
                {
                    using (var writer = new JsonTextWriter(stream))
                    {
                        serializer.Serialize(writer, new { Name = "test", Value = 1 });
                    }
                }

                var buffer = memStream.ToArray();
                Assert.NotEmpty(buffer);


            }

        }

        [Fact]
        public void PatchDocumentTest()
        {
            var d = new PatchDocument
            {
                Name = "test",
                IsTrue = false
            };
            var services = new ServiceCollection();
            services.BuildServiceProvider().SetRootProvider();
            var patch = d.ToPatchDocument<IPatchDocument>();
            Assert.NotNull(patch);
            Assert.Equal("test", patch.Name);
            patch.Name = "test2";
            patch.IsTrue = true;
            var jsonPatch = patch.AsJsonPatch<IPatchDocument>();
            Assert.Equal(2, jsonPatch.Operations.Count);
        }
    }

    public interface IPatchDocument : IPatchableDocument
    {
        string Name { get; set; }
        bool IsTrue { get; set; }
    }

    public class PatchDocument : IPatchDocument
    {
        public string Name
        {
            get;
            set;
        }

        public bool IsTrue
        {
            get;
            set;
        }


    }
}
