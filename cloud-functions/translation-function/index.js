const { TranslationServiceClient } = require("@google-cloud/translate").v3;
const functions = require("@google-cloud/functions-framework");

const client = new TranslationServiceClient();
const location = process.env.TRANSLATION_LOCATION || "global";

functions.http("translateMenuItem", async (req, res) => {
  res.set("Access-Control-Allow-Origin", process.env.ALLOWED_ORIGIN || "*");
  if (req.method === "OPTIONS") {
    res.set("Access-Control-Allow-Methods", "POST");
    res.set("Access-Control-Allow-Headers", "Content-Type");
    return res.status(204).send("");
  }

  const text = req.body?.text;
  const targetLanguage = req.body?.targetLanguage;
  if (!text || !targetLanguage) {
    return res.status(400).json({ error: "text and targetLanguage are required" });
  }

  try {
    const projectId = process.env.GOOGLE_CLOUD_PROJECT
      || process.env.GCP_PROJECT
      || process.env.PROJECT_ID
      || await client.getProjectId();

    const [response] = await client.translateText({
      parent: `projects/${projectId}/locations/${location}`,
      contents: [text],
      mimeType: "text/plain",
      targetLanguageCode: targetLanguage
    });

    return res.json({
      translatedText: response.translations?.[0]?.translatedText || text
    });
  } catch (error) {
    console.error(error);
    return res.status(500).json({
      error: "Translation API call failed",
      details: error.message
    });
  }
});
