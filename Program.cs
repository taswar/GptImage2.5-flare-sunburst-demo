using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI.Images;
using System.Net.Http.Headers;
using System.Text.Json;
 
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();
 
var endpoint = new Uri(config["AZURE_AI_ENDPOINT"]
    ?? throw new InvalidOperationException(
        "AZURE_AI_ENDPOINT is not set. Run: dotnet user-secrets set \"AZURE_AI_ENDPOINT\" \"<your-endpoint>\""));
 
// After reading endpoint from configuration:
var apiKey = config["AZURE_AI_API_KEY"]
    ?? throw new InvalidOperationException("AZURE_AI_API_KEY is not set.");

var imageEditApiVersion = config["AZURE_AI_IMAGE_EDIT_API_VERSION"] ?? "2025-04-01-preview";
const string flareDeployment = "gpt-image-2.5-flare";
const string sunburstDeployment = "gpt-image-2.5-sunburst";

var azureClient = new AzureOpenAIClient(endpoint, new ApiKeyCredential(apiKey));
var flareClient = azureClient.GetImageClient(flareDeployment);
var sunburstClient = azureClient.GetImageClient(sunburstDeployment);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

Uri CreateImageRequestUri(string deployment, string operation, string apiVersion) => new(
    $"{endpoint.ToString().TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(deployment)}/images/{operation}?api-version={Uri.EscapeDataString(apiVersion)}");

async Task<byte[]> ReadImageResponseAsync(HttpResponseMessage response, Uri requestUri)
{
    var responseBody = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        throw new HttpRequestException(
            $"Azure image request failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). " +
            $"Endpoint: {requestUri}\nResponse: {responseBody}");
    }

    using var json = JsonDocument.Parse(responseBody);
    var base64Image = json.RootElement
        .GetProperty("data")[0]
        .GetProperty("b64_json")
        .GetString();

    if (string.IsNullOrWhiteSpace(base64Image))
    {
        throw new InvalidOperationException("Azure image response did not contain data[0].b64_json.");
    }

    return Convert.FromBase64String(base64Image);
}

async Task<byte[]> GenerateKitchenDesignAsync(string description)
{
    GeneratedImage image = await flareClient.GenerateImageAsync(description, new ImageGenerationOptions
    {
        Size = new GeneratedImageSize(1536, 864)
    });
    return image.ImageBytes.ToArray();
}
 
async Task<byte[]> ReviseKitchenDesignAsync(byte[] currentImage, string revisionInstruction)
{
    var requestUri = CreateImageRequestUri(flareDeployment, "edits", imageEditApiVersion);
    using var form = new MultipartFormDataContent();
    using var imageContent = new ByteArrayContent(currentImage);
    imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    form.Add(imageContent, "image[]", "kitchen-design.png");
    form.Add(new StringContent(revisionInstruction), "prompt");
    form.Add(new StringContent("1536x864"), "size");

    using var response = await httpClient.PostAsync(requestUri, form);
    return await ReadImageResponseAsync(response, requestUri);
}
 
Console.WriteLine("**************************** Case 1 ************************************");
Console.WriteLine("**** Home Design — Iterative Generation That Holds Its Shape ***********");

// First pass: the initial description.
byte[] kitchenV1 = await GenerateKitchenDesignAsync(
    "A modern kitchen with dark cabinets and a large center island, professional " +
    "interior photography style, natural daylight.");
await File.WriteAllBytesAsync("kitchen-v1.png", kitchenV1);
Console.WriteLine("Saved: kitchen-v1.png");
 
// Second pass: an edit against the same image, not a fresh prompt from scratch.
byte[] kitchenV2 = await ReviseKitchenDesignAsync(kitchenV1,
    "Make the lighting brighter and add natural wood accents to the cabinetry and " +
    "island. Keep the layout, camera angle, and overall composition the same.");
await File.WriteAllBytesAsync("kitchen-v2.png", kitchenV2);
Console.WriteLine("Saved: kitchen-v2.png");
Console.WriteLine("**************************** END Case 1 **********************************");

async Task<byte[]> PreviewDestinationAsync(string description)
{
    GeneratedImage preview = await flareClient.GenerateImageAsync(description, new ImageGenerationOptions
    {
        Size = GeneratedImageSize.W1024xH1024,        
    });
    return preview.ImageBytes.ToArray();
}
 
async Task<byte[]> FinalizeItineraryImageAsync(string description)
{
    GeneratedImage final = await sunburstClient.GenerateImageAsync(description, new ImageGenerationOptions
    {
        Size =  new GeneratedImageSize(1536, 864)        
    });
    return final.ImageBytes.ToArray();
}

Console.WriteLine("**************************** Case 2 ************************************");  
Console.WriteLine("**** Travel Planning — Exploration with Flare, Final Asset with Sunburst *****");
// Exploration phase: quick, cheap previews as the traveler narrows down ideas.
string[] explorationPrompts =
[
    "A quiet coastal town in Portugal, whitewashed buildings, golden hour, travel photography style.",
    "Same coastal town, but with a small boat harbor visible and outdoor cafes along the water.",
];
 
foreach (var prompt in explorationPrompts)
{
    byte[] preview = await PreviewDestinationAsync(prompt);
    await File.WriteAllBytesAsync($"preview-{explorationPrompts.ToList().IndexOf(prompt)}.png", preview);
}
 

Console.WriteLine("Traveler picked the second preview - finalizing the itinerary card for day 3.");
 
 
byte[] finalCard = await FinalizeItineraryImageAsync(
    "A polished travel itinerary hero image: a coastal Portuguese town with a small boat harbor, " +
    "outdoor cafes, golden hour lighting, labeled 'Day 3: Coastal Harbor Town', clean readable text overlay.");
await File.WriteAllBytesAsync("itinerary-day3-final.png", finalCard);
Console.WriteLine("Saved: itinerary-day3-final.png");
Console.WriteLine("**************************** END Case 2 **********************************");


async Task<byte[]> GenerateLessonDiagramAsync(string concept)
{
    var prompt = $"""
        A clean, labeled educational diagram explaining: {concept}.
        Simple flat illustration style, high contrast, readable labels,
        suitable for a student-facing tutoring app.
        """;
 
    GeneratedImage diagram = await flareClient.GenerateImageAsync(prompt, new ImageGenerationOptions
    {
        Size = new GeneratedImageSize(1024, 1024)     
    });
    return diagram.ImageBytes.ToArray();
}
 
// Simulated turns of a tutoring conversation - each turn shifts what the diagram needs to show.
var lessonTurns = new[]
{
    "how photosynthesis converts sunlight into energy in a plant cell",
    "how photosynthesis differs between C3 and C4 plants",
};
 
Console.WriteLine("**************************** Case 3 ************************************");
Console.WriteLine("**** Education — Diagrams That Evolve With the Lesson ******************");  
foreach (var concept in lessonTurns)
{
    byte[] diagram = await GenerateLessonDiagramAsync(concept);
    string fileName = $"lesson-diagram-{lessonTurns.ToList().IndexOf(concept)}.png";
    await File.WriteAllBytesAsync(fileName, diagram);
    Console.WriteLine($"Saved: {fileName} (\"{concept}\")");
}

Console.WriteLine("**************************** END Case 3 **********************************");

// Final campaign asset - precision and legible text matter more than speed here.
var campaignPrompt = """
    A wide-format e-commerce banner ad for a summer sale. Clean minimalist studio
    background in soft pastel blue. A pair of white running shoes floating at a
    slight angle, dramatic soft shadow beneath. Bold, perfectly legible headline
    text at the top reading "SUMMER SALE - 30% OFF" in a clean modern sans-serif
    font, white text with subtle drop shadow.
    """;
 
Console.WriteLine("**************************** Case 4 ************************************");  
Console.WriteLine("**** Retail Campaign Creative — Legible Text, Consistent Batches *******");

GeneratedImage campaignAsset = await sunburstClient.GenerateImageAsync(campaignPrompt, new ImageGenerationOptions
{
    Size = new GeneratedImageSize(1536, 864)        
});
await File.WriteAllBytesAsync("campaign-summer-sale-banner.png", campaignAsset.ImageBytes.ToArray());
Console.WriteLine("Saved: campaign-summer-sale-banner.png");
 
// Catalog batch - volume matters, Flare keeps cost and latency down across many SKUs.
var catalogItems = new[]
{
    new { Sku = "SKU-1001", Description = "a matte black ceramic coffee mug" },
    new { Sku = "SKU-1002", Description = "a glossy red ceramic coffee mug" },
    new { Sku = "SKU-1003", Description = "a sage green ceramic coffee mug" },
};
 
const string catalogStyleTemplate = """
    A studio product photo of {0} on a plain white seamless background, soft even
    lighting from the upper left, subtle reflection beneath, centered composition,
    e-commerce catalog style, no props, no text.
    """;
 
foreach (var item in catalogItems)
{
    var prompt = string.Format(catalogStyleTemplate, item.Description);
    GeneratedImage image = await flareClient.GenerateImageAsync(prompt, new ImageGenerationOptions
    {
        Size = new GeneratedImageSize(1024, 1024)        
    });
    string outPath = $"catalog-{item.Sku}.png";
    await File.WriteAllBytesAsync(outPath, image.ImageBytes.ToArray());
    Console.WriteLine($"Saved: {outPath}");
}
Console.WriteLine("**************************** END Case 4 **********************************");

Console.WriteLine("**************************** Case 5 ************************************"); 
Console.WriteLine("******** Virtual Try-On — Multi-Turn Editing Without Drift *************");

async Task<byte[]> EditProductPhotoAsync(byte[] sourceImage, string editInstruction)
{
    var requestUri = CreateImageRequestUri(sunburstDeployment, "edits", imageEditApiVersion);
    using var form = new MultipartFormDataContent();
    using var imageContent = new ByteArrayContent(sourceImage);
    imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
    form.Add(imageContent, "image[]", "product-photo.png");
    form.Add(new StringContent(editInstruction), "prompt");
    form.Add(new StringContent("1024x1024"), "size");

    using var response = await httpClient.PostAsync(requestUri, form);
    return await ReadImageResponseAsync(response, requestUri);
}
 
byte[] originalPhoto = await File.ReadAllBytesAsync("customer-tryon-photo.png");
 
// Successive edits - each one builds on the last, and Sunburst is built to keep
// the customer's pose, framing, and identity consistent across all of them.
byte[] navyJacket = await EditProductPhotoAsync(originalPhoto,
    "Replace the jacket the person is wearing with a navy blue version of the same jacket style. " +
    "Keep the person's pose, face, and background exactly the same.");
await File.WriteAllBytesAsync("tryon-navy.png", navyJacket);
 
byte[] navyJacketOutdoors = await EditProductPhotoAsync(navyJacket,
    "Change the background to an outdoor city street setting. Keep the jacket color and the " +
    "person's pose and appearance exactly the same.");
await File.WriteAllBytesAsync("tryon-navy-outdoors.png", navyJacketOutdoors);
 
Console.WriteLine("Saved: tryon-navy.png, tryon-navy-outdoors.png");
Console.WriteLine("**************************** END Case 5 **********************************");

