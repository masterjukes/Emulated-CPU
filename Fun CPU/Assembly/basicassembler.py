import sys
import os


import argparse
from getopt import error
from inspect import stack

parser = argparse.ArgumentParser(description="Process one or more input files into one output file.")
parser.add_argument("-S", "--use-stdlib",action="store_true",help="Dont use stdlib")
parser.add_argument("-F", "--single-file",action="store_true",help="Use single file")
parser.add_argument("input", help="Input file")
parser.add_argument("output", help="Output file")
args = parser.parse_args()


print("Input files:", args.input)
input_file = []
if args.single_file:
    i = args.input
    if not i.endswith(".s"):
        print(f"Error: {i} is not a .s file", 0)
        exit(1)
    else:
        with open(i) as f:
            print(f"Reading {i}")
            input_file.extend(f.readlines())
else:
    folder = args.input
    makefile_path = os.path.join(folder, "make.f")
    if not os.path.isfile(makefile_path):
        print(f"Error: make.f not found in {folder}")
        exit(1)

    print(f"Reading makefile order from: {makefile_path}")
    with open(makefile_path) as mf:
        file_list = [line.strip() for line in mf if line.strip()]
        for filename in file_list:
            if not filename.endswith(".s"):
                print(f"Error: '{filename}' in make.f is not a .s file")
                exit(1)

            if not filename.startswith("@"):
                filepath = os.path.join(folder, filename)
                print("notstdl")
            else:
                filepath = os.path.abspath(os.path.dirname(__file__)) + "/stdlibs/" + filename.strip("@\n")

            # Ensure the file exists
            if not os.path.isfile(filepath):
                print(f"Error: listed file '{filename}' does not exist in folder {filepath}")
                exit(1)

            print(f"Reading {filepath}")

            # Append lines to input_file
            with open(filepath) as f:
                input_file.extend(f.readlines())





file_output = args.output
mem_offset = 0
output = []
in_macro = False
in_macro_name = ""

opcode_map = {
    # ARITHMETIC BYTE (3 registers: dest, src1, src2)
    "ADD":   {"value": 0x01, "operands": ["register", "register", "register"]},
    "SUB":   {"value": 0x02, "operands": ["register", "register", "register"]},
    "MUL":   {"value": 0x03, "operands": ["register", "register", "register"]},
    "DIV":   {"value": 0x04, "operands": ["register", "register", "register"]},
    "MOD":   {"value": 0x05, "operands": ["register", "register", "register"]},

    # LOGIC BYTE
    "AND":   {"value": 0x06, "operands": ["register", "register", "register"]},
    "OR":    {"value": 0x07, "operands": ["register", "register", "register"]},
    "XOR":   {"value": 0x08, "operands": ["register", "register", "register"]},
    "NOT":   {"value": 0x09, "operands": ["register", "register"]},  # dest, src
    "SHL":   {"value": 0x0A, "operands": ["register", "register", "register"]},
    "SHR":   {"value": 0x0B, "operands": ["register", "register", "register"]},

    # ARITHMETIC DWORD
    "ADDL":  {"value": 0x0C, "operands": ["register", "register", "register"]},
    "SUBL":  {"value": 0x0D, "operands": ["register", "register", "register"]},
    "MULL":  {"value": 0x0E, "operands": ["register", "register", "register"]},
    "DIVL":  {"value": 0x0F, "operands": ["register", "register", "register"]},
    "MODL":  {"value": 0x10, "operands": ["register", "register", "register"]},

    # LOGIC DWORD
    "ANDL":  {"value": 0x11, "operands": ["register", "register", "register"]},
    "ORL":   {"value": 0x12, "operands": ["register", "register", "register"]},
    "XORL":  {"value": 0x13, "operands": ["register", "register", "register"]},
    "NOTL":  {"value": 0x14, "operands": ["register", "register"]},
    "SHLL":  {"value": 0x15, "operands": ["register", "register", "register"]},
    "SHRL":  {"value": 0x16, "operands": ["register", "register", "register"]},

    # JUMPS AND COMPARES
    "CMP":   {"value": 0x17, "operands": ["register", "register"]},
    "JMP":   {"value": 0x18, "operands": ["register"]},
    "JEQ":   {"value": 0x19, "operands": ["register"]},
    "JNE":   {"value": 0x1A, "operands": ["register"]},
    "JGT":   {"value": 0x1B, "operands": ["register"]},
    "JLT":   {"value": 0x1C, "operands": ["register"]},
    "JGE":   {"value": 0x1D, "operands": ["register"]},
    "JLE":   {"value": 0x1E, "operands": ["register"]},

    # STACK
    "CALL":  {"value": 0x1F, "operands": ["register"]},
    "RET":   {"value": 0x20, "operands": []},
    "PUSH":  {"value": 0x21, "operands": ["register"]},
    "POP":   {"value": 0x22, "operands": ["register"]},

    # DATA MOVEMENT BYTE
    "STORE": {"value": 0x23, "operands": ["register", "register"]},
    "LOAD":  {"value": 0x24, "operands": ["register", "register"]},
    "MOV":   {"value": 0x25, "operands": ["register", "immediate"]},

    # DATA MOVEMENT DWORD
    "STOREL":{"value": 0x26, "operands": ["register", "register"]},
    "LOADL": {"value": 0x27, "operands": ["register", "register"]},
    "MOVL":  {"value": 0x28, "operands": ["register", "immediate4"]},

    # SYSTEM
    "NOP":   {"value": 0x29, "operands": []},
    "HALT":  {"value": 0x2A, "operands": []},

    # INCREMENT AND DECREMENT
    "INC":   {"value": 0x2F, "operands": ["register"]},
    "DEC":   {"value": 0x30, "operands": ["register"]},
    
    "JMPI":  {"value": 0x31, "operands": ["immediate4"]},
    "JNEI":  {"value": 0x32, "operands": ["immediate4"]},
    "JEQI":  {"value": 0x33, "operands": ["immediate4"]},
    "JGTI":  {"value": 0x34, "operands": ["immediate4"]},
    "JLTI":  {"value": 0x35, "operands": ["immediate4"]},
    "JGEI": {"value": 0x36, "operands": ["immediate4"]},
    "JLEI": {"value": 0x37, "operands": ["immediate4"]},
    
    "CMPI":  {"value": 0x38, "operands": ["register", "immediate4"]},
    "CALLI": {"value": 0x39, "operands": ["immediate4"]},

    "DATA":   {"value": 0x77, "operands": ["immediate"]},
}
label_map = {}
macro_map = {}

def error(msg: str, line_num: int):
    print(f"Error on Line {line_num+1}: {msg}")
    exit(1)

class Parser:

    @staticmethod
    def parse_macro(_line: str, line_num: int):
        name = _line.split(" ")[0].strip('%\n')
        print(f"name: {name}")
        if macro_map.get(name.strip("\n"), None) is None:
            if name != "end":
                args = _line.split("%" + name)[1].strip(" \n").split(" ")
                print(f"name: {name} args: {args}")
                macro_map[name] = {"args": args, "lines": []}
                global in_macro, in_macro_name
                in_macro = True
                in_macro_name = name
            else:
                print(f"{in_macro_name} ENDED")
                in_macro = False
                in_macro_name = ""
        else:
            error(f"Error: Macro {name} already defined", line_num)


    @staticmethod
    def parse_label(_line: str, line_num: int):
        output.append(_line.strip('\n'))

    @staticmethod
    def parse_call(_line: str, line_num: int):
        macro_name = _line.strip("*\n").split(" ")[0]
        if macro_map.get(macro_name, None) is None:
            error(f"Error: Macro {macro_name} not defined", line_num)
        else:
            expected_args = macro_map[macro_name]["args"]
            real_args = _line.split("*" + macro_name)[1].strip(" \n").split(" ")
            if len(expected_args) != len(real_args):
                error(f"Error: Macro {macro_name} called with wrong number of arguments", line_num)
            for q_line in macro_map[macro_name]["lines"]:
                expanded_line = q_line
                for idx, arg in enumerate(real_args):
                    print(f"line: {q_line}")
                    expanded_line = expanded_line.replace(expected_args[idx], real_args[idx])
                    print(f"Replaced {expected_args[idx]} with {real_args[idx]}")
                    print(f"line: {q_line.replace(expected_args[idx], real_args[idx])}")
                temp2.append(expanded_line)
                









    @staticmethod
    def parse_instruction(_line: str, line_num: int):
        operands = [tok for tok in _line.strip().split(" ") if tok]

        if not operands or operands[0] == "\n":
            return


        instruction = opcode_map.get(operands[0].upper(), "ERROR: unknown opcode")
        if instruction == "ERROR: unknown opcode":
            error(f"Error: unknown opcode: {operands[0]}", line_num)
        else:
            op_name = operands[0]
            if instruction["value"] != 0x77:
                output.append(instruction["value"])
            operands.pop(0)
            expectedOpands = instruction["operands"]
            if len(operands) != len(expectedOpands):
                opname = instruction['name'] if 'name' in instruction else op_name
                error(f"Error: opcode {opname} expects {len(expectedOpands)} operands, got {len(operands)}", line_num)
            for i, operand in enumerate(operands):
                if expectedOpands[i] == "immediate":

                    if operand.startswith("#"):
                        output.append(operand.strip('#\n'))
                    elif operand.startswith("'"):
                        output.append("'" + operand.strip("'\n"))
                    elif operand.startswith("0x"):
                        output.append(operand.strip('\n'))
                    elif operand.startswith("0b"):
                        output.append(operand.strip('\n'))
                    elif operand.startswith("."):
                        output.append(operand.strip('\n'))
                        output.append(0)
                        output.append(0)
                        output.append(0)
                    else: error(f"Error: immediate operand expected but found: {operand}", line_num)
                elif expectedOpands[i] == "immediate4":

                    if operand.startswith("#"):
                        myval = int(operand.strip('#\n'))
                        myval = myval.to_bytes(4, 'little', signed=False)
                        output.extend(list(myval))
                    elif operand.startswith("'"):
                        output.append("'" + operand.strip("'\n"))
                        output.append(0)
                        output.append(0)
                        output.append(0)
                    elif operand.startswith("0x"):
                        myval = int(operand.strip('\n'), 16)
                        myval = myval.to_bytes(4, 'little', signed=False)
                        output.extend(list(myval))
                    elif operand.startswith("0b"):
                        myval = int(operand.strip('\n'), 2)
                        myval = myval.to_bytes(4, 'little', signed=False)
                        output.extend(list(myval))
                    elif operand.startswith("."):
                        output.append(operand.strip('\n'))
                        output.append(0)
                        output.append(0)
                        output.append(0)
                    else: error(f"Error: immediate operand expected but found: {operand}", line_num)

                elif expectedOpands[i] == "register":
                    if operand.startswith("%"):
                        output.append(operand.strip('%\n'))
                    else: error(f"Error: Register operand expected but found: {operand}", line_num)
                else:
                    pass









    @staticmethod
    def parse_proginfo(_line: str, line_num: int):
        if _line.startswith("@offset"):
            global mem_offset
            mem_offset = int(_line.split()[1])
        elif _line.startswith("@const"):
            name = _line.split()[1]
            value = _line.split()[2]
            for i, lne in enumerate(temp2):
                temp2[i] = lne.replace(name, value)
                
        elif (_line.startswith("@use")):
            pass
            
            
        else:
            error(f"Error: no known program info found denoted by: {_line}", line_num)


    @staticmethod
    def parse_line(_line: str, line_num: int):
        if _line.startswith("@"):
            Parser.parse_proginfo(_line, line_num)
        elif _line.startswith(":"):
            Parser.parse_label(_line, line_num)
        elif _line.startswith("%"):
            Parser.parse_macro(_line, line_num)
        else:
            Parser.parse_instruction(_line, line_num)

class ParseHLL:
    labelsNeededCount = 0
    linesAdded = 0
    labelStack = list()
    exprStack = list()
    @staticmethod
    def parse_line(_line: str, line_num: int):
        currShift = 0
        jmpCmpOppositeMap = {">": "JLE", 
                     "<": "JGE",
                     ">=": "JLT",
                     "<=": "JGT",
                     "==": "JNE",
                     "!=": "JEQ"}
        
        temporaryInstructionBuffer = list()

            
        
        
        if(_line.startswith("if")):
            exprArg1 = _line.split()[1]
            comparison = _line.split()[2]
            exprArg3 = _line.split()[3]
            
            if(comparison in jmpCmpOppositeMap):
                if(exprArg1.startswith("%") and exprArg3.startswith("%")):
                    regA = exprArg1.strip("%\n")
                    regB = exprArg3.strip("%\n")
                    temporaryInstructionBuffer.append(f"CMP %{regA} %{regB}\n")
                    temporaryInstructionBuffer.append(f"{jmpCmpOppositeMap[comparison]}I .L{ParseHLL.labelsNeededCount}\n")
                    ParseHLL.labelStack.append(f"L{ParseHLL.labelsNeededCount}")
                    ParseHLL.labelsNeededCount += 1
                    ParseHLL.exprStack.append("if")
                if(exprArg1.startswith("%") and not exprArg3.startswith("%")):
                    regA = exprArg1.strip("%\n")
                    imm = exprArg3
                    temporaryInstructionBuffer.append(f"CMPI %{regA} {imm}\n")
                    temporaryInstructionBuffer.append(f"{jmpCmpOppositeMap[comparison]}I .L{ParseHLL.labelsNeededCount}\n")
                    ParseHLL.labelStack.append(f"L{ParseHLL.labelsNeededCount}")
                    ParseHLL.labelsNeededCount += 1
                    ParseHLL.exprStack.append("if")

                else:
                    error(f"Error: if statement expects two registers, got {exprArg1} and {exprArg3}", line_num)
            else:
                error(f"Error: if statement expects a comparison operator, got {comparison}", line_num)
        
        elif(_line.startswith("else")):
            if(len(ParseHLL.labelStack) > 0):
                expectedLabel = ParseHLL.labelStack.pop()
                newLabel = f"L{ParseHLL.labelsNeededCount}"
                ParseHLL.labelStack.append(newLabel)
                temporaryInstructionBuffer.append(f"JMPI .{newLabel}\n")
                ParseHLL.labelsNeededCount += 1
                temporaryInstructionBuffer.append(f":{expectedLabel}\n")
            else:
                error(f"Error: else statement without matching if statement", line_num)
        
        
        elif(_line.startswith("end")):
            if(len(ParseHLL.labelStack) > 0):
                if(ParseHLL.exprStack[-1] == "if"):
                    ParseHLL.exprStack.pop()
                    expectedLabel = ParseHLL.labelStack.pop()
                    temporaryInstructionBuffer.append(f":{expectedLabel}\n")
                elif(ParseHLL.exprStack[-1] == "while"):
                    ParseHLL.exprStack.pop()
                    expectedLabel = ParseHLL.labelStack.pop()
                    expectedLabel2 = ParseHLL.labelStack.pop()
                    temporaryInstructionBuffer.append(f"JMPI .{expectedLabel2}\n")
                    temporaryInstructionBuffer.append(f":{expectedLabel}\n")
                temporaryInstructionBuffer.append("NOP\n")
                    
            else:
                error(f"Error: end statement without matching if statement", line_num)
                
        elif _line.startswith("while"):
            exprArg1 = _line.split()[1]
            comparison = _line.split()[2]
            exprArg3 = _line.split()[3]
            
            if(comparison in jmpCmpOppositeMap):
                if(exprArg1.startswith("%") and exprArg3.startswith("%")):
                    regA = exprArg1.strip("%\n")
                    regB = exprArg3.strip("%\n")
                    temporaryInstructionBuffer.append(f":L{ParseHLL.labelsNeededCount}\n")
                    ParseHLL.labelStack.append(f"L{ParseHLL.labelsNeededCount}")
                    ParseHLL.labelsNeededCount += 1
                    temporaryInstructionBuffer.append(f"CMP %{regA} %{regB}\n")
                    temporaryInstructionBuffer.append(f"{jmpCmpOppositeMap[comparison]}I .L{ParseHLL.labelsNeededCount}\n")
                    ParseHLL.labelStack.append(f"L{ParseHLL.labelsNeededCount}")
                    ParseHLL.labelsNeededCount += 1
                    ParseHLL.exprStack.append("while")
                if(exprArg1.startswith("%") and not exprArg3.startswith("%")):
                    regA = exprArg1.strip("%\n")
                    imm = exprArg3
                    temporaryInstructionBuffer.append(f":L{ParseHLL.labelsNeededCount}\n")
                    ParseHLL.labelStack.append(f"L{ParseHLL.labelsNeededCount}")
                    ParseHLL.labelsNeededCount += 1
                    temporaryInstructionBuffer.append(f"CMPI %{regA} {imm}\n")
                    temporaryInstructionBuffer.append(f"{jmpCmpOppositeMap[comparison]}I .L{ParseHLL.labelsNeededCount}\n")
                    ParseHLL.labelStack.append(f"L{ParseHLL.labelsNeededCount}")
                    ParseHLL.labelsNeededCount += 1
                    ParseHLL.exprStack.append("while")
                    
                    

        else:
            return lines
        
        
        lines[line_num + ParseHLL.linesAdded] = temporaryInstructionBuffer[0]
        for i in range(1, len(temporaryInstructionBuffer)):
            lines.insert(line_num + i + ParseHLL.linesAdded, temporaryInstructionBuffer[i])
            
        ParseHLL.linesAdded += len(temporaryInstructionBuffer)-1
        return lines
        
        
                    
                    
        







preHLLlines = input_file.copy()
lines = input_file.copy()
shift = 0

for i, line in enumerate(preHLLlines):
    line = line.replace("    ", "")
    temp = ParseHLL.parse_line(line, i)



filer = open("temp.s", "w")
for line in temp:
    filer.write(line)



for i, line in enumerate(lines):
    if line.startswith("%"):
        Parser.parse_macro(line, i)
        temp.pop(i - shift)
        shift += 1
    elif in_macro and not line.startswith("%"):
        macro_map[in_macro_name]["lines"].append(line.strip('\n'))
        temp.pop(i - shift)
        shift += 1
print(temp)
shift = 0
temp2 = []
for i, line in enumerate(temp):
    if line.startswith("*"):
        Parser.parse_call(line, i)
    else:
            temp2.append(line)
for i, line in enumerate(temp2):
    try:
        Parser.parse_line(line, i)
    except:
        Parser.parse_line(line, i)
        error("python error", i)
        




gencode = []


for val in output:
    if isinstance(val, int):
        if val < 256:
            gencode.append(val)
        else:
            gencode.extend(val.to_bytes(4, byteorder='little', signed=False))
    elif val.startswith("0x"):
        if int(val, 16) < 256:
            gencode.append(int(val, 16))
        else:
            gencode.extend(int(val, 16).to_bytes(4, byteorder='little', signed=False))
    elif val.startswith("0b"):
        if int(val, 2) < 256:
            gencode.append(int(val, 2))
        else:
            gencode.extend(int(val, 2).to_bytes(4, byteorder='little', signed=False))
    elif val.startswith("'"):
        gencode.append(ord(val.strip("'")))
    elif val.startswith("."):
        gencode.append(val)
    elif val.startswith(":"):
        gencode.append(val)
    else:
        if int(val) < 256:
            gencode.append(int(val))
        else:
            gencode.extend(int(val).to_bytes(4, byteorder='little', signed=False))


byteOutput = bytearray()

for index, byte in enumerate(gencode):
    if isinstance(byte, str):
        if byte.startswith(":"):
            name = byte.strip(":")
            if not name.startswith("."):
                name = "." + name
            label_map[name] = index + mem_offset
            gencode.pop(index)

dontWrite = False
counterDw = 0
for index, byte in enumerate(gencode):
    if isinstance(byte, str):
        if byte.startswith("."):
            by = label_map.get(byte, "ERR")
            if by != "ERR":
                gencode[index] = label_map[byte]
                byteOutput.extend(label_map[byte].to_bytes(4, byteorder='little', signed=False))
                dontWrite = True
            else:
                error(f"Error: unknown label: {byte}", index)
    else:
        if not dontWrite:
            byteOutput.append(byte)
        else:
            counterDw += 1
            if counterDw == 3:
                dontWrite = False
                counterDw = 0



print(f"wrote {len(byteOutput)} bytes to {file_output}")

with open(file_output, "wb") as f:
    f.write(byteOutput)
