

// Dropzone
var dropzonePreviewNode = document.querySelector("#property-preview-list");
dropzonePreviewNode.id = "";
if (dropzonePreviewNode) {
    var previewTemplate = dropzonePreviewNode.parentNode.innerHTML;
    dropzonePreviewNode.parentNode.removeChild(dropzonePreviewNode);
    var dropzone = new Dropzone(".property-dropzone", {
        url: 'https://httpbin.org/post',
        method: "post",
        previewTemplate: previewTemplate,
        previewsContainer: "#property-preview",
    });
}