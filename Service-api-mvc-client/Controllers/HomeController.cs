using EPiServer.Integration.Client.Models;
using EPiServer.Integration.Client.Models.Catalog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service_api_mvc_client.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Service_api_mvc_client.Controllers
{
    public class HomeController : Controller
    {
        public HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:44328/");
            var fields = new Dictionary<string, string>
                    {
                        { "grant_type", "password" },
                        { "username", "admin" },
                        { "password", "store" }
                    };
            var response = client.PostAsync("/episerverapi/token", new FormUrlEncodedContent(fields)).Result;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var adminToken = JObject.Parse(content).GetValue("access_token");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken.ToString());
            }
            return client;
        }

        public ActionResult Index()
        {
            HttpClient client = GetHttpClient();

            var result = client.GetAsync("/episerverapi/commerce/entries/0/50").Result.Content.ReadAsStringAsync().Result;

            Entries entries = JsonConvert.DeserializeObject<Entries>(result);
            
            return View(entries);
        }

        public ActionResult SingleEntry(string Code)
        {
            EntryViewModel viewModel = new EntryViewModel();

            HttpClient client = GetHttpClient();

            var result = client.GetAsync($"/episerverapi/commerce/entries/{Code}").Result.Content.ReadAsStringAsync().Result;
            viewModel.SelectedEntry = JsonConvert.DeserializeObject<Entry>(result);

            var priceResult = client.GetAsync($"/episerverapi/commerce/entries/{Code}/prices").Result.Content.ReadAsStringAsync().Result;
            viewModel.Prices = JsonConvert.DeserializeObject<IEnumerable<Price>>(priceResult);

            var nodesResult = client.GetAsync("/episerverapi/commerce/catalog/Fashion/nodes").Result.Content.ReadAsStringAsync().Result;
            IEnumerable<Node> rootNodes = JsonConvert.DeserializeObject<IEnumerable<Node>>(nodesResult);
            foreach(var node in rootNodes)
            {
                viewModel.Nodes = FlattenNodes(node, client);
            }
            return View(viewModel);
        }

        private IEnumerable<Node> FlattenNodes(Node node, HttpClient client)
        {
            yield return node;
            foreach(var child in node.Children)
            {
                var nodeResult = client.GetAsync(child.Href).Result.Content.ReadAsStringAsync().Result;
                var childNode = JsonConvert.DeserializeObject<Node>(nodeResult);
                foreach (var flattenedNode in FlattenNodes(childNode, client))
                {
                    yield return flattenedNode;
                }
            }
        }

        public ActionResult CreateEntry()
        {
            var entry = new Entry()
            {
                Catalog = "Fashion",
                EntryType = "Variation",
                MetaClass = "Shirt_Variation",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(100),
                IsActive = true,
                Variation = new VariationProperties
                {
                    MaxQuantity = 100,
                    MinQuantity = 0,
                    TaxCategory = "VAT",
                    Weight = 5
                },
                Size = "M",
                Color = "Blue",
                Brand = "Acme",
                CanBeMonogrammed = false
            };
            return View(entry);
        }

        public ActionResult SubmitEntry(Entry entry)
        {
            entry.MetaFields.Add(new MetaFieldProperty()
            {
                Name = "Brand",
                Type = "ShortString",
                Data = new List<MetaFieldData>()
                {
                    new MetaFieldData()
                    {
                        Language = "en",
                        Value = entry.Brand
                    }
                }
            });
            entry.MetaFields.Add(new MetaFieldProperty()
            {
                Name = "Color",
                Type = "ShortString",
                Data = new List<MetaFieldData>()
                {
                    new MetaFieldData()
                    {
                        Language = "en",
                        Value = entry.Color
                    }
                }
            });
            entry.MetaFields.Add(new MetaFieldProperty()
            {
                Name = "Size",
                Type = "ShortString",
                Data = new List<MetaFieldData>()
                {
                    new MetaFieldData()
                    {
                        Language = "en",
                        Value = entry.Size
                    }
                }
            });
            entry.MetaFields.Add(new MetaFieldProperty()
            {
                Name = "CanBeMonogrammed",
                Type = "Boolean",
                Data = new List<MetaFieldData>()
                {
                    new MetaFieldData()
                    {
                        Language = "en",
                        Value = entry.CanBeMonogrammed.ToString()
                    }
                }
            });
            HttpClient client = GetHttpClient();
            string json = JsonConvert.SerializeObject(entry);
            var result = client.PostAsync("/episerverapi/commerce/entries", new StringContent(json, Encoding.UTF8, "application/json")).Result;

            return RedirectToAction("Index");
        }
    }
}