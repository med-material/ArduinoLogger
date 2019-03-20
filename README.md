# Arduino Logger
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
 * `END LOG` : Specifies that the logging has finished. The log is then written to a CSV file.

For example files following this format, see [ReactionSynchTests](https://github.com/med-material/ArduinoReactionSynchTests).

Built for the RTII course at AAU (Unity 2018.3.2f1)
