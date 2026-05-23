%store_w %reg #imm
MOVL %15 #imm
STOREL %reg %15
%end

%store_b %reg #imm
MOV %15 #imm
STORE %reg %15
%end


%load_w %reg #imm
MOVL %15 #imm
LOADL %reg %15
%end

%load_b %reg #imm
MOVL %15 #imm
LOAD %reg %15
%end


%store_wi #addr #imm
MOVL %14 #addr
MOVL %15 #imm
STOREL %14 %15
%end

%store_bi #addr #imm
MOVL %14 #addr
MOVL %15 #imm
STORE %14 %15
%end


%load_wi #addr #imm
MOVL %14 #addr
MOVL %15 #imm
LOADL %14 %15
%end

%load_bi #addr #imm
MOVL %14 #addr
MOVL %15 #imm
LOAD %14 %15
%end

%store_wr #addr %reg
MOVL %15 #addr
STOREL %15 %reg
%end

%store_br #addr %reg
MOVL %15 #addr
STORE %15 %reg
%end


%load_wr #addr %reg
MOVL %15 #addr
LOADL %15 %reg
%end

%load_br #addr %reg
MOVL %15 #addr
LOAD %15 %reg
%end

%jmpi #imm
MOVL %15 #imm
JMP %15
%end

%jeqi #imm
MOVL %15 #imm
JEQ %15
%end

%jnei #imm
MOVL %15 #imm
JNE %15
%end

%jgti #imm
MOVL %15 #imm
JGT %15
%end

%jlti #imm
MOVL %15 #imm
JLT %15
%end

%jgei #imm
MOVL %15 #imm
JGE %15
%end

%jlei #imm
MOVL %15 #imm
JLE %15
%end

%cmpi %reg #imm
MOVL %15 #imm
CMP %reg %15
%end

%addli %dest %reg #imm
MOVL %15 #imm
ADDL %dest %reg %15
%end

%addi %dest %reg #imm
MOVL %15 #imm
ADD %dest %reg %15
%end

%subli %dest %reg #imm
MOVL %15 #imm
SUBL %dest %reg %15
%end

%subi %dest %reg #imm
MOVL %15 #imm
SUB %dest %reg %15
%end

%mulli %dest %reg #imm
MOVL %15 #imm
MULL %dest %reg %15
%end

%muli %dest %reg #imm
MOVL %15 #imm
MUL %dest %reg %15
%end

%divli %dest %reg #imm
MOVL %15 #imm
DIVL %dest %reg %15
%end

%divi %dest %reg #imm
MOVL %15 #imm
DIV %dest %reg %15
%end

%modli %dest %reg #imm
MOVL %15 #imm
MODL %dest %reg %15
%end

%modi %dest %reg #imm
MOVL %15 #imm
MOD %dest %reg %15
%end

%andli %dest %reg #imm
MOVL %15 #imm
ANDL %dest %reg %15
%end

%andi %dest %reg #imm
MOVL %15 #imm
AND %dest %reg %15
%end

%orli %dest %reg #imm
MOVL %15 #imm
ORL %dest %reg %15
%end

%ori %dest %reg #imm
MOVL %15 #imm
OR %dest %reg %15
%end

%xorli %dest %reg #imm
MOVL %15 #imm
XORL %dest %reg %15
%end

%xori %dest %reg #imm
MOVL %15 #imm
XOR %dest %reg %15
%end

%shlli %dest %reg #imm
MOVL %15 #imm
SHLL %dest %reg %15
%end

%shli %dest %reg #imm
MOVL %15 #imm
SHL %dest %reg %15
%end

%shrli %dest %reg #imm
MOVL %15 #imm
SHRL %dest %reg %15
%end

%shri %dest %reg #imm
MOVL %15 #imm
SHR %dest %reg %15
%end

%func_call #addr
MOVL %15 #addr
CALL %15
%end






