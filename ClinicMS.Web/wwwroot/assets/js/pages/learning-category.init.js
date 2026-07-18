

// Course Images
var dropzonePreviewNode = document.querySelector("#category-preview-list");
dropzonePreviewNode.id = "";
if (dropzonePreviewNode) {
    var previewTemplate = dropzonePreviewNode.parentNode.innerHTML;
    dropzonePreviewNode.parentNode.removeChild(dropzonePreviewNode);
    var dropzone = new Dropzone(".category-dropzone", {
        url: 'https://httpbin.org/post',
        method: "post",
        previewTemplate: previewTemplate,
        previewsContainer: "#category-preview",
    });
}
