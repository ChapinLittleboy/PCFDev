// wwwroot/download.js
window.downloadFileFromStream = async (fileName, contentStreamRef) => {
    const arrayBuffer = await contentStreamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer], {
        type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName || "download.xlsx";
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
};
