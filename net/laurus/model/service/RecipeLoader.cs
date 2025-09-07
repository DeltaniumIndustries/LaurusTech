using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LaurusTech.net.laurus.model.service
{
    public static class RecipeLoader
    {
        public static List<Recipe> Load(string path)
        {
            var json = File.ReadAllText("json/" + path);
            return JsonConvert.DeserializeObject<List<Recipe>>(json);
        }
    }
}
