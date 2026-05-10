const { Firestore, FieldValue } = require("@google-cloud/firestore");
const vision = require("@google-cloud/vision");
const functions = require("@google-cloud/functions-framework");

const db = new Firestore();
const visionClient = new vision.ImageAnnotatorClient();

functions.cloudEvent("processMenuUpload", async (cloudEvent) => {
  const message = cloudEvent.data?.message;
  const payload = JSON.parse(Buffer.from(message.data, "base64").toString("utf8"));
  const { restaurantId, menuId, bucket, objectName } = payload;

  const [result] = await visionClient.textDetection(`gs://${bucket}/${objectName}`);
  const ocrText = result.fullTextAnnotation?.text || "";

  const restaurantRef = db.collection("restaurants").doc(restaurantId);
  const menuRef = restaurantRef.collection("menus").doc(menuId);

  await menuRef.set({
    ocrText,
    status: "pending",
    updatedAt: FieldValue.serverTimestamp()
  }, { merge: true });

  await restaurantRef.set({
    status: "pending",
    updatedAt: FieldValue.serverTimestamp()
  }, { merge: true });
});
