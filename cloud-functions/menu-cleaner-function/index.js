const { Firestore, FieldValue } = require("@google-cloud/firestore");
const functions = require("@google-cloud/functions-framework");

const db = new Firestore();

functions.http("cleanPendingMenus", async (req, res) => {
  const reprocess = req.query.reprocess === "true" || req.body?.reprocess === true;
  const restaurants = reprocess
    ? await db.collection("restaurants").get()
    : await db.collection("restaurants").where("status", "==", "pending").get();
  let cleaned = 0;

  for (const restaurant of restaurants.docs) {
    const menus = reprocess
      ? await restaurant.ref.collection("menus").get()
      : await restaurant.ref.collection("menus").where("status", "==", "pending").get();

    for (const menu of menus.docs) {
      const data = menu.data();
      const items = parseMenuItems(data.ocrText || "");
      const previous = data.items || [];

      await menu.ref.set({
        items,
        previousItems: previous,
        status: "completed",
        updatedAt: FieldValue.serverTimestamp()
      }, { merge: true });
      cleaned += 1;
    }

    await restaurant.ref.set({
      status: "ready",
      updatedAt: FieldValue.serverTimestamp()
    }, { merge: true });
  }

  return res.json({ cleaned });
});

function parseMenuItems(text) {
  const lines = text
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean);

  const items = [];
  let pendingName = "";

  for (const line of lines) {
    if (/menu$/i.test(line)) {
      continue;
    }

    const sameLine = line.match(/^(.+?)\s+(?:(?:EUR|\u20ac|\u5143)\s*)?(\d+(?:[.,]\d{1,2})?)\s*(?:EUR|\u20ac|\u5143)?$/i);
    if (sameLine && hasUsefulName(sameLine[1])) {
      items.push(toItem(sameLine[1], sameLine[2], line));
      pendingName = "";
      continue;
    }

    const priceOnly = line.match(/^(?:(?:EUR|\u20ac|\u5143)\s*)?(\d+(?:[.,]\d{1,2})?)\s*(?:EUR|\u20ac|\u5143)?$/i);
    if (priceOnly && pendingName) {
      items.push(toItem(pendingName, priceOnly[1], line));
      pendingName = "";
      continue;
    }

    if (hasUsefulName(line)) {
      pendingName = line;
    }
  }

  if (items.length > 0) {
    return items;
  }

  const compactItems = [];
  const matches = text.matchAll(/([\p{L}\p{N}\s.'-]{2,40}?)\s*(?:EUR|\u20ac|\u5143)?\s*(\d+(?:[.,]\d{1,2})?)\s*(?:EUR|\u20ac|\u5143)/gu);
  for (const match of matches) {
    if (hasUsefulName(match[1])) {
      compactItems.push(toItem(match[1], match[2], match[0]));
    }
  }

  return compactItems;
}

function toItem(name, price, sourceLine) {
  return {
    name: name.replace(/[.]+$/g, "").trim(),
    price: Number(price.replace(",", ".")),
    currency: sourceLine.includes("\u5143") ? "CNY" : "EUR"
  };
}

function hasUsefulName(value) {
  const trimmed = value.trim();
  return trimmed.length > 1 && !/^(EUR|\u20ac|\u5143)$/i.test(trimmed) && /\p{L}/u.test(trimmed);
}
