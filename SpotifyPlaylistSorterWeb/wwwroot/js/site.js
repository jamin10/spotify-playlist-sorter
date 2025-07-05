// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
window.queuePlaylist = async function(playlistId, antiForgeryToken) {
    const formData = new FormData();
    formData.append('playlistId', playlistId);

    const response = await fetch('/MessageQueue/AnalysePlaylist', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': antiForgeryToken
        },
        body: formData
    });
    if (response.ok) {
        alert('Playlist queued for analysis!');
    } else {
        alert('Failed to queue playlist.');
    }
}