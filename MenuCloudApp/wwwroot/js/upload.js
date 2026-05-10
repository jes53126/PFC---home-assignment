const form = document.getElementById("uploadForm");
const progressList = document.getElementById("progressList");

form?.addEventListener("submit", (event) => {
  event.preventDefault();
  progressList.innerHTML = "";

  const files = document.getElementById("files").files;
  const restaurantName = document.getElementById("restaurantName").value;
  [...files].forEach((file) => uploadFile(restaurantName, file));
});

function uploadFile(restaurantName, file) {
  const row = document.createElement("div");
  row.className = "upload-row";
  row.innerHTML = `<span>${file.name}</span><progress value="0" max="100"></progress><small>Waiting</small>`;
  progressList.appendChild(row);

  const data = new FormData();
  data.append("restaurantName", restaurantName);
  data.append("files", file);

  const xhr = new XMLHttpRequest();
  xhr.open("POST", "/Upload/Upload");
  xhr.upload.onprogress = (event) => {
    if (!event.lengthComputable) return;
    const percent = Math.round((event.loaded / event.total) * 100);
    row.querySelector("progress").value = percent;
    row.querySelector("small").textContent = `${percent}%`;
  };
  xhr.onload = () => {
    row.querySelector("small").textContent = xhr.status < 400 ? "Queued for processing" : "Upload failed";
  };
  xhr.onerror = () => {
    row.querySelector("small").textContent = "Upload failed";
  };
  xhr.send(data);
}
