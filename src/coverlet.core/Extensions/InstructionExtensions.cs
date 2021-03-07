using Mono.Cecil;
using Mono.Cecil.Cil;


namespace Coverlet.Core.Extensions
{
    // Extension methods that can smooth out small differences between similar CIL
    // instructions, such as "ldarg.0" as opposed to "ldarg 0".
    public static class InstructionExtensions
    {
        #region ldarg

        public static bool IsLdarg(this Instruction instruction, int argumentIndex)
        {
            return IsLdarg(instruction, out int actualArgumentIndex) &&
                   argumentIndex == actualArgumentIndex;
        }


        public static bool IsLdarg(this Instruction instruction)
        {
            return IsLdarg(instruction, out int _);
        }


        public static bool IsLdarg(this Instruction instruction, out int argumentIndex)
        {
            if (instruction.OpCode == OpCodes.Ldarg ||
                instruction.OpCode == OpCodes.Ldarg_S)
            {
                argumentIndex = ((ParameterReference) instruction.Operand).Index;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldarg_0)
            {
                argumentIndex = 0;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldarg_1)
            {
                argumentIndex = 1;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldarg_2)
            {
                argumentIndex = 2;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldarg_3)
            {
                argumentIndex = 3;
                return true;
            }
            else
            {
                argumentIndex = -1;
                return false;
            }
        }

        #endregion


        #region ldc.i4

        public static bool IsLdc_I4(this Instruction instruction)
        {
            return IsLdc_I4(instruction, out int value);
        }


        public static bool IsLdc_I4(this Instruction instruction, int value)
        {
            return IsLdc_I4(instruction, out int actualValue) &&
                   value == actualValue;
        }


        public static bool IsLdc_I4(this Instruction instruction, out int value)
        {
            if (instruction.OpCode == OpCodes.Ldc_I4 ||
                instruction.OpCode == OpCodes.Ldc_I4_S)
            {
                value = (int) instruction.Operand;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_0)
            {
                value = 0;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_1)
            {
                value = 1;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_2)
            {
                value = 2;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_3)
            {
                value = 3;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_4)
            {
                value = 4;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_5)
            {
                value = 5;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_6)
            {
                value = 6;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_7)
            {
                value = 7;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldc_I4_8)
            {
                value = 8;
                return true;
            }
            else
            {
                value = 0;
                return false;
            }
        }

        #endregion


        #region ldloc

        public static bool IsLdloc(this Instruction instruction, int localVariableIndex)
        {
            return IsLdloc(instruction, out int actualLocalVariableIndex) &&
                   localVariableIndex == actualLocalVariableIndex;
        }


        public static bool IsLdloc(this Instruction instruction)
        {
            return IsLdloc(instruction, out int _);
        }


        public static bool IsLdloc(this Instruction instruction, out int localVariableIndex)
        {
            if (instruction.OpCode == OpCodes.Ldloc ||
                instruction.OpCode == OpCodes.Ldloc_S)
            {
                localVariableIndex = ((VariableDefinition) instruction.Operand).Index;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldloc_0)
            {
                localVariableIndex = 0;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldloc_1)
            {
                localVariableIndex = 1;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldloc_2)
            {
                localVariableIndex = 2;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Ldloc_3)
            {
                localVariableIndex = 3;
                return true;
            }
            else
            {
                localVariableIndex = -1;
                return false;
            }
        }

        #endregion Ldloc


        #region stloc

        public static bool IsStloc(this Instruction instruction, int localVariableIndex)
        {
            return IsStloc(instruction, out int actualLocalVariableIndex) &&
                   localVariableIndex == actualLocalVariableIndex;
        }


        public static bool IsStloc(this Instruction instruction)
        {
            return IsStloc(instruction, out int _);
        }


        public static bool IsStloc(this Instruction instruction, out int localVariableIndex)
        {
            if (instruction.OpCode == OpCodes.Stloc ||
                instruction.OpCode == OpCodes.Stloc_S)
            {
                localVariableIndex = ((VariableDefinition) instruction.Operand).Index;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Stloc_0)
            {
                localVariableIndex = 0;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Stloc_1)
            {
                localVariableIndex = 1;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Stloc_2)
            {
                localVariableIndex = 2;
                return true;
            }
            else if (instruction.OpCode == OpCodes.Stloc_3)
            {
                localVariableIndex = 3;
                return true;
            }
            else
            {
                localVariableIndex = -1;
                return false;
            }
        }

        #endregion
    }
}
