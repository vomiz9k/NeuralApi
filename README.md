# NeuralApi

Segments image. Input: png image. Output: 256x256x1 .mat file.


Api routes:
- /api/upload - upload file to be proccessed. Returns: File Id.
- /api/{id}/status - check status of proccessing file with this Id.
- /api/{id}/download - download result of file proccessing.
