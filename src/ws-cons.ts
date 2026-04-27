// deno run -N ws-cons.ts

const token = '<CONSUMER_TOKEN>'
const socket = new WebSocket("<WS_API_ENDPOINT>");
socket.addEventListener("open", () => {
  console.log(socket.readyState);
  socket.send(token);
});
socket.addEventListener("message", (event) => {
  console.log(JSON.parse(event.data));
});
