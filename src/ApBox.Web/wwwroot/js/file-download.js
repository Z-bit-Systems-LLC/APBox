// File download utility functions for ApBox

window.downloadFile = (fileName, contentType, data) => {
    // Convert byte array to blob
    const blob = new Blob([new Uint8Array(data)], { type: contentType });
    
    // Create download link
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    
    // Trigger download
    document.body.appendChild(link);
    link.click();
    
    // Cleanup
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};

window.downloadFileFromText = (fileName, contentType, text) => {
    const blob = new Blob([text], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    
    document.body.appendChild(link);
    link.click();
    
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};