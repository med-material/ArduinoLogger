# Arduino Logger (Windows/Mac)
![RTII Arduino Logger](https://raw.githubusercontent.com/med-material/RTIIArduinoLogger/master/rtii-arduino-logger-image.png)

The Arduino Logger logs Arduino serial output to *.csv files (buildable as a standalone application).
[Download here](https://github.com/med-material/ArduinoLogger/releases/) (Windows, Mac, OS X)

## How to use it
The Arduino logger parses any Arduino Serial output beginning with `#` and ending with `\n` and which adheres to the following format:

```
#BEGIN LOG (col=3,sep=comma,label=mytest)
#Header1,Header2,Header3
#Data1,Data2,Data3
#Data1,Data2,Data3
#Data1,Data2,Data3
#Data1,Data2,Data3
...
#END LOG
```

 * `BEGIN LOG` : Specifies that the logging will begin.
 * `col`: Specifies the number of columns. If there is a mismatch, the Arduino Logger Interrupts logging.
 * `sep`: Specifies the separator. Supported values are `tab` and `comma`
 * `label`: an arbitrary label (ASCII characters only, no punctuation).
 * `Header1,Header2,Header3` : The line after BEGIN Log is alwas assumed to be a header.
 * `#Data1,Data2,Data3` : Your data. By default, the Arduino logger will continuously log incoming data to disk until you close it.
 * `END LOG` : Specifies that the logging has finished. This is necessary if you use the Arduino Logger to log to the database.

For example files following this format, see [ReactionSynchTests](https://github.com/med-material/ArduinoReactionSynchTests) or [EDA-IBI-Pressure](https://github.com/med-material/ArduinoEDA-IBI-Pressure).

Built for the RTII course at AAU (Unity 2018.3.2f1)
