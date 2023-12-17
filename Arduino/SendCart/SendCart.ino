#include <Wire.h>
#include <inttypes.h>

#define VALUE_TO_CORRECT 0

// Arduino constants
static constexpr uint8_t i2c_buff_size = 32;
static constexpr uint8_t serial_buff_size = 64;

// EEPROM constants
static constexpr int page_count = VALUE_TO_CORRECT;
static constexpr int page_size = VALUE_TO_CORRECT;
static constexpr int i2c_buff_per_page = page_size / i2c_buff_size;
static constexpr uint8_t eeprom_addr = VALUE_TO_CORRECT;

uint8_t pageData[page_size] = {};
uint16_t pageAddr = 0;

// Send a two bytes address via I²C which selects an address within the EEPROM.
void write_addr(uint16_t addr) {
  /* FIXME: Implement sending the two bytes of the address individually. */
}

// When the receiver sums on its side, it can just add this sum and check that is 0.
uint16_t sum_complement(const uint8_t data[page_size]) {
        uint16_t sum = 0;
        for (int i = 0; i<page_size; i += 2) {
          sum += (data[i+1] << 8) | data[i];
        }
        return -sum;
}

void setup() {
  Serial.begin(115200);
  pinMode(LED_BUILTIN, OUTPUT);
  Wire.begin();
  Wire.setClock(400000);
}

/*
 * Read data from the Serial port until a full message is read.
 * This function is blocking and will not return early, use with caution.
 * Use '\n' as a delimiter for a complete message, making it exit
 * early if it is encountered before the count is done (invalid message).
 */
void read_message(char *buffer, const uint8_t match_len) {
  uint8_t read_bytes = 0;
  do {
    /* FIXME: Read the correct amount of bytes from the serial, without going over
              and stopping at the delimiter. */
  } while(read_bytes < match_len || Serial.peek() == '\n');
  if (Serial.peek() == '\n')
    Serial.read(); // Consume the delimiter if it is there.
}

void loop() {
  char ret[4];
  // Wait until we receive the start message.
  do {
    memset(ret, 0, 4);
    read_message(ret, 2);
  } while (strncmp(ret, "GO", 2) != 0);

  // Reset the EEPROM internal address to 0. It autoincrements after.
  /* FIXME: Start a communication and send the address as defined by
            the datasheet for a selective/random read. */

  digitalWrite(LED_BUILTIN, HIGH);
  /*
   * We don't know the size of the cartridge, and it could be the whole EEPROM.
   * Read and send all the pages (64kiB), it should not impact the final PNG.
   */
  for (int i = 0; i < page_count; i++) {
    memset(pageData, 0, page_size);
    // Arduino I2C buffer is 32 bytes, so we need multiple requests per page.
    for (int j = 0; j < i2c_buff_per_page; j++) {
      /* FIXME: Request and read the data from the EEPROM.
          Tips: - You will need two calls with Wire.<func>.
                - Check the datasheet to see if you need to send the address again */
    }

    // Try to send the full page via serial until the recipient ACKs.
    uint16_t page_checksum = sum_complement(pageData);
    do {
      // The internal Serial buffer is smaller than a page, so send in bursts.
      size_t written = 0;
      /* FIXME: Send the whole page via serial and its checksum.
          Tips: - You are trying to send bytes, not characters !
                - You cannot send the whole array at once : track your position.
                - When sending data, Serial assumes it is all sent.*/

      memset(ret, 0, 4);
      read_message(ret, 3);
    } while(strncmp(ret, "ACK", 3) != 0);
  }

  digitalWrite(LED_BUILTIN, LOW);
}
