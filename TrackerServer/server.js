const WebSocket = require('ws');
const config = require('./config.json');
const readline = require('readline');
const server = new WebSocket.Server({ host: config.serverAddress, port: config.serverPort });

const trackers = new Map();
const trackerReceivers = new Map();

server.on('connection', (socket) => {
  socket.on('message', (message) => {
    const data = JSON.parse(message);

    if (data.type === 'tracker') {
      trackers.set(data.id, socket);
      console.log(`Tracker ${data.id} connected.`);
    } else if (data.type === 'trackerreceiver') {
      trackerReceivers.set(data.id, socket);
      console.log(`TrackerReceiver ${data.id} connected.`);
    } else {
      // Assuming the received data is from a tracker
      const trackerData = data;

      trackerData.forEach((item) => {
        const receiverId = item.id;
        const receiverSocket = trackerReceivers.get(receiverId);
        if (receiverSocket) {
          receiverSocket.send(JSON.stringify(item));
        }
      });
    }
  });

  socket.on('close', () => {
    for (const [id, trackerSocket] of trackers) {
      if (trackerSocket === socket) {
        trackers.delete(id);
        console.log(`Tracker ${id} disconnected.`);
        break;
      }
    }

    for (const [id, receiverSocket] of trackerReceivers) {
      if (receiverSocket === socket) {
        trackerReceivers.delete(id);
        console.log(`TrackerReceiver ${id} disconnected.`);
        break;
      }
    }
  });
});


// Listen for keypresses
readline.emitKeypressEvents(process.stdin);
process.stdin.setRawMode(true);

// Handle keypress events
process.stdin.on('keypress', (str, key) => {
  if (key.name === 'd') {
    // Data to send when 'D' is pressed
    const data = { x: -1, y: -1, id: -1 };

    // Send data to all connected trackerReceivers
    trackerReceivers.forEach((receiverSocket) => {
      if (receiverSocket.readyState === WebSocket.OPEN) {
        receiverSocket.send(JSON.stringify(data));
      }
    });

    console.log("Sent {x: -10, y: -10, id: -1} to all trackerReceivers.");
  }

  // Exit on ctrl + c
  if (key.ctrl && key.name === 'c') {
    process.exit();
  }
});

console.log(`WebSocket server is running on ${config.serverAddress}:${config.serverPort}`);