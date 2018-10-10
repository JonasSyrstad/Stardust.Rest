using Newtonsoft.Json;
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
    }
}
