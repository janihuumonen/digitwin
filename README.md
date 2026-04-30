# IoT digital twin

## Description
**Digital twin of a fish tank**
- Raspberry Pi Pico measures water level and temperature.
- Deno HTTP/WebSocket server authenticates and relays data.
- Unity3D mirrors the fish tank state using the telemetry.


## Architecture

### System overview
- Device (data producer)
- Backend service (data router)
- UI client application (data consumer)

### Device architecture
- Based on a previous [IoT group project](https://github.com/janihuumonen/iotemb.git)
- **Raspberry Pi Pico 2 W** (running MicroPython firmware)
  - AccessPoint mode for configuration (static IP)
    - Booted into if configuration data is not found
    - Serves an HTML form for entering configuration
    - On form submit, saves configuration as a JSON file and reboots
  - Station mode for normal operation
    - Connects to the configured WLAN AP
    - Communicates to the backend service via HTTPS
    - Uses a shared secret token as authentication
    - Measures voltages over the known resistances in a loop
      - Averages measurements over a short interval
      - Translates voltages to height and temperature values
      - Sends a POST request when either value has changed sufficiently
    - Bootsel button triggers a configuration reset and reboot
- **Sensors**
  - Thermistor in series with a known resistance
  - Insulated solid core copper wires in series with a known resistance
    - 2 parallel pairs, each ending at a different height
    - Insulation stripped at the ends

### Backend architecture
- **Reverse proxy**
  - Terminates TLS (HTTPS/WSS)
  - Performs path-based routing
  - Forwards traffic to internal service
- **Backend service**
  - Deno HTTP/WebSocket server
  - Manages remote connections from device and UI
  - Authenticates connections by verifying shared secret tokens
  - Routes data between device and UI clients

### UI architecture (Unity3D)
- **GameObject**
  - Represents the state of the real-world object
  - Glass walls (Cubes)
    - Material: transparent, reflective, specular highlights
  - Water (Cube)
    - Material: translucent, colored
    - Box Collider (dynamic dimensions relative to the water cube)
    - Bubbles (ParticleSystem)
      - Material(particles): transparent, specular highlights
      - Contained inside the box collider
      - Lightly randomized movement and size
- **MonoBehaviour**
  - Attached to the GameObject
  - Connects to the backend service via WSS
  - Authenticates, subscribes to messages
  - Uses a shared secret token as authentication
  - Updates GameObject state based on received data
    - Sets the size and position of the Cube representing water
    - Sets the number and rate of particles for the ParticleSystem representing bubbles


## Feature roadmap

### Client application alerts
- alert when values are out of safe range
  - configurable in the client

### Control commands
- client application sets thresholds
- server sends state of connections / connected peers

### Device calibration
- shortpress
  - start collecting data
  - led on
- shortpress again
  - find centroids in collected dataset
    - use midway points between them to set ranges (lo,mid,hi)
    - use distances between them to set hysteresis region width
    - led off
  - write config

### Device factory reset
- longpress
  - button down
    - start timer
    - timer runs out
      - set led blinking
  - button up
    - if timer done
      - reset config
      - reboot

### Device control logic
- oxygenation pump voltage control
  - PWM output pin + MOSFET

