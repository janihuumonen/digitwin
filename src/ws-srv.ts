// deno run -N ws-srv.ts

const TOKEN_CONSUMER = '<CONSUMER_TOKEN>';
const TOKEN_PRODUCER = '<PRODUCER_TOKEN>';

class Srv {
  private clients = [];
  private consumer = null;
  private producer = null;

  public async handle(req) {
    if (req.headers.get("upgrade") != "websocket") {
      if ("POST" === req.method) {
        try {
          const body = await req.json(); //JSON.parse(await req.text());
          if (body.token === TOKEN_PRODUCER) {
            console.log(body);
            if (this.consumer) this.consumer.send(JSON.stringify({height:body.height, temp:body.temp}));
          }
        } catch(e) { /*json SyntaxError*/ }
        return new Response('OK\n', { status: 200 });
      }
      else return new Response(null, { status: 426 }); // HTTP 426 Upgrade Required
    }
    const { socket, response } = Deno.upgradeWebSocket(req);

    socket.addEventListener("open", () => {
      this.clients.push(socket);
      console.log("a client connected!",this.clients.length);
    });

    socket.addEventListener("close", () => {
      const index = this.clients.indexOf(socket);
      if (index > -1) this.clients.splice(index, 1);
      if (socket === this.consumer) this.consumer = null;
      else if (socket === this.producer) this.producer = null;
      console.log("a client disconnected!",this.clients.length);
      console.log('consumer:',!!this.consumer,'producer:',!!this.producer);
    });

    socket.addEventListener("message", (event) => {
      // TODO: check token
      switch (socket) {
        case this.consumer:
          console.log('from consumer:',event.data);
          if (this.producer) this.producer.send(event.data);
          break;
        case this.producer:
          console.log('from producer:',event.data);
          if (this.consumer) this.consumer.send(event.data);
          break;
        default:
          switch (event.data) {
            case TOKEN_CONSUMER:
              if (!this.consumer) {
                this.consumer = socket;
                console.log('consumer registered');
              } else socket.close();
              break;
            case TOKEN_PRODUCER:
              if (!this.producer) {
                this.producer = socket;
                console.log('producer registered');
              } else socket.close();
              break;
            default:
              socket.close();
          }
      }
    });

    return response;
  }
}

const srv = new Srv();
Deno.serve(
  { port: 3000, hostname: "127.0.0.1" },
  r => srv.handle(r)
);
