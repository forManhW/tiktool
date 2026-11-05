const { contextBridge, ipcRenderer } = require("electron");
contextBridge.exposeInMainWorld("tiktoolDesktop", { launchGame: (game) => ipcRenderer.invoke("launch-game", game), pickImage: () => ipcRenderer.invoke("pick-image") });
