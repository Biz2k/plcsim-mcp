---
name: plcsim_data_types
description: Instructions for handling Siemens data types (Time, String, BCD, Struct, Array) through the MCP server.
---

# Working with Data Types in PLCSIM Advanced MCP Server

Interacting with the PLCSIM Advanced simulator imposes certain restrictions on data type usage. Unlike TIA Portal, the simulator API communicates exclusively through primitive types (`EPrimitiveDataType`).

Below are the rules for AI agents on how to handle various data types in TIA Portal via our server.

## 1. Basic (Primitive) Types
For basic data types (`Bool`, `Int8`, `Int16`, `Int32`, `Float`, `Double`, etc.), use them directly. When calling `plcsim_write_tag`, the server will automatically parse the string value into the correct type if you provide the `dataType`.
- **Example**: `value: "True", dataType: "Bool"`
- **Example**: `value: "14.5", dataType: "Float"`

## 2. Strings (String / WString)
Although in the Siemens API strings consist of a byte/character array and a separate header (Max Length, Actual Length), our MCP server **encapsulates** this complexity.
- Use the built-in processing to work with strings.
- Always specify `dataType: "String"` or `dataType: "WString"`.
- **Write**: `plcsim_write_tag(tagName="MyTag", value="Hello", dataType="String")`
- **Read**: `plcsim_read_tag` with the `String` or `WString` type specification will read the bytes and return the text itself.

## 3. Time (Time, LTime, S5Time)
In Siemens controllers, the `Time` type is stored as an `Int32` (milliseconds), and `LTime` as an `Int64` (nanoseconds).
- Our MCP server supports parsing human-readable Siemens Time format (e.g.: `10s`, `1h20m`, `500ms`).
- For writing, pass a string with the time and make sure to specify `dataType: "Time"`. The server will automatically convert `10s` to `10000` and write it as an `Int32`.
- **Example**: `value: "1h20m", dataType: "Time"`

## 4. BCD (Binary Coded Decimal) / Hex
The BCD format (often used in older S7-300/400 projects and 7-segment displays) does not have a built-in type in the API. It is stored as a `Word` (`UInt16`) or `DWord` (`UInt32`).
- You must **manually convert** the decimal number to BCD (its hexadecimal representation) before sending.
- For example, to write the value `123` in BCD:
  - Form the Hex string: `0x0123`
  - Convert Hex `0123` to a decimal number: `291`.
  - Write `291` to the controller with the `UInt16` type.
- Similarly, when reading: upon receiving `291` from the PLC, you must convert it to hex (`0x0123`) and interpret it as the decimal `123`.

## 5. Structures (Struct, UDT)
The PLCSIM Advanced API **does not support** reading/writing entire structures (`Struct` / `UDT`) in a single call.
- You must access structure fields individually.
- Access is done using dot notation: `MyStruct.Member1`, `MyMotor.Status.Running`.
- For optimization, use the batch processing tools: `plcsim_batch_read` / `plcsim_batch_write`, passing a list of tag names.

## 6. Arrays (Array)
Similarly to structures, arrays cannot be read or written entirely as a single tag.
- Index array elements individually using square brackets: `MyArray[0]`, `MyArray[1]`.
- For exchanging large arrays, use `plcsim_batch_read` and `plcsim_batch_write`.
