
%putchar #char #color
MOVL %0 #17154752
MOV %1 #1
STORE %0 %1
MOVL %0 0x104B000
MOV %1 #char
STORE %0 %1
INC %0
MOV %1 #color
STORE %0 %1
%end

%rputchar %mem %char #color
STORE %mem %char
INC %mem
MOV %15 #color
STORE %mem %15
INC %mem
%end

%rcputchar %char #color
PUSH %1
INC %0
STORE %0 %char
INC %0
MOV %1 #color
STORE %0 %1
POP %1
%end
%cputchar #char #color
INC %0
MOV %1 #char
STORE %0 %1
INC %0
MOV %1 #color
STORE %0 %1
%end

%getkey %reg
PUSH %0
MOVL %0 #17154753
LOAD %reg %0
POP %0
%end


 
