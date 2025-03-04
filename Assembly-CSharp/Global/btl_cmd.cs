﻿using FF9;
using System;
using System.Collections.Generic;
using System.Linq;
using Memoria;
using Memoria.Data;
using Memoria.Scripts;
using UnityEngine;
using NCalc;

// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable EmptyConstructor
// ReSharper disable ArrangeThisQualifier
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable StringCompareToIsCultureSpecific
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable NotAccessedField.Local
// ReSharper disable ConvertToConstant.Global
// ReSharper disable ConvertToConstant.Local
// ReSharper disable UnusedParameter.Global
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable InconsistentNaming

public class btl_cmd
{
    /* Notes on commands:
	Each BTL_DATA has 6 potential commands at the same time.
	Only the first 3 are initialized for enemies by default (btl_cmd.InitCommand).
	With Memoria: we 6 commands are used even for enemies (for uses of btl_scrp.SetCharacterData(id == 114)).
	btl.cmd[0] -> normal commands (including berserk/confuse)
	btl.cmd[1] -> counter-attacks
	btl.cmd[2] -> reserved for death/stone animation (cmd_no == 60/62, sub_no == 0)
	btl.cmd[3] -> first cast of a double-cast command or Spear (saved duplicate) or Eidolon phantom
	btl.cmd[4] -> reserved for trance animation (cmd_no == 59, sub_no == 0)
	btl.cmd[5] -> reserved for reraise animation (cmd_no == 61, sub_no == 0)
	*/
    public btl_cmd()
    {
    }

    #region Memoria custom API
    public static bool IsAttackShortRange(CMD_DATA cmd)
    {
        // Custom usage of "aa.Type & 0x8" (unused by default): flag for short range attacks
        // One might want to check using "cmd.aa.Info.VfxIndex" and "cmd.aa.Vfx2" instead
        if (cmd.aa == null)
            return false;
        if (cmd.regist == null)
            return false;
        if (cmd.regist.weapon != null && (cmd.regist.weapon.Category & Param.WPN_CATEGORY_SHORT_RANGE) == 0)
            return false;
        if (Configuration.Battle.CustomBattleFlagsMeaning == 1 && (cmd.AbilityType & 0x8) != 0)
            return true;
        if (Configuration.Battle.CustomBattleFlagsMeaning != 1 && (cmd.regist.bi.player == 0 || cmd.sub_no == 176))
            return true;
        return false;
    }

    public static bool SpendSpareChangeGil(BattleUnit caster, CMD_DATA cmd)
    {
        PARTY_DATA partyState = FF9StateSystem.Common.FF9.party;
        UInt32 cost = cmd.Power * (UInt32)caster.Level; // default
        if (Configuration.Battle.SpareChangeGilSpentFormula.Length > 0)
        {
            Expression e = new Expression(Configuration.Battle.SpareChangeGilSpentFormula);
            e.EvaluateFunction += NCalcUtility.commonNCalcFunctions;
            e.EvaluateParameter += NCalcUtility.commonNCalcParameters;
            NCalcUtility.InitializeExpressionUnit(ref e, caster, "Caster");
            NCalcUtility.InitializeExpressionCommand(ref e, new BattleCommand(cmd));
            Int64 val = NCalcUtility.ConvertNCalcResult(e.Evaluate(), -1);
            if (val >= 0)
                cost = (UInt32)val;
        }
        if (cost > partyState.gil)
            return false;
        partyState.gil -= cost;
        return true;
    }
    #endregion

    public static void ClearCommand(CMD_DATA cmd)
    {
        cmd.next = null;
        cmd.aa = null;
        cmd.tar_id = 0;
        cmd.cmd_no = BattleCommandId.None;
        cmd.sub_no = 0;
        cmd.info.Reset();
    }

    public static void ClearReflecData(CMD_DATA cmd)
    {
        for (Int32 index = 0; index < 4; ++index)
            cmd.reflec.tar_id[index] = 0;
    }

    public static void InitCommandSystem(FF9StateBattleSystem btlsys)
    {
        btlsys.cur_cmd_list.Clear();
        btlsys.cmd_status = 2;
        btlsys.cmd_queue.regist = btlsys.cmd_escape.regist = null;
        ClearCommand(btlsys.cmd_queue);
        ClearCommand(btlsys.cmd_escape);
        btl_cmd.next_cmd_delay = 0;
    }

    public static void InitSelectCursor(FF9StateBattleSystem btlsys)
    {
        UInt32 num1 = (UInt32)((FF9StateSystem.Battle.FF9Battle.btl_cnt & 15) << 8);
        List<Vector3> vector3List1 = new List<Vector3>();
        List<Vector3> vector3List2 = new List<Vector3>();
        List<Color32> color32List = new List<Color32>();
        List<Int32> intList = new List<Int32>();
        Vector3 vector3_1 = new Vector3(0.0f, 0.0f, 0.0f);
        vector3List1.Add(vector3_1);
        Int32 num2 = 0;
        while (num2 < 3)
        {
            Single f = (Single)(num1 / 4096.0 * 360.0);
            Vector3 vector3_2 = new Vector3(vector3_1.x - (Int32)(68.0 * Mathf.Cos(f)), vector3_1.y - 133f, vector3_1.z - (Int32)(68.0 * Mathf.Sin(f)));
            vector3_2.y *= -1f;
            vector3List1.Add(vector3_2);
            ++num2;
            num1 = (num1 + 1265U) % 4096U;
        }
        vector3List2.Add(vector3List1[0]);
        vector3List2.Add(vector3List1[2]);
        vector3List2.Add(vector3List1[1]);
        vector3List2.Add(vector3List1[0]);
        vector3List2.Add(vector3List1[3]);
        vector3List2.Add(vector3List1[2]);
        vector3List2.Add(vector3List1[0]);
        vector3List2.Add(vector3List1[1]);
        vector3List2.Add(vector3List1[3]);
        vector3List2.Add(vector3List1[1]);
        vector3List2.Add(vector3List1[2]);
        vector3List2.Add(vector3List1[3]);
        for (Int32 index = 0; index < vector3List2.Count; ++index)
        {
            Byte num3 = (Byte)Mathf.Floor(index / 3f);
            Byte num4 = index >= 9 ? (Byte)160 : (Byte)(188 - 60 * num3);
            color32List.Add(new Color32(num4, num4, 0, Byte.MaxValue));
            intList.Add(index);
        }
        Mesh mesh = new Mesh
        {
            vertices = vector3List2.ToArray(),
            triangles = intList.ToArray(),
            colors32 = color32List.ToArray()
        };
        mesh.RecalculateNormals();
        GameObject gameObject = btlsys.s_cur = new GameObject("selectCursor");
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        Material material = new Material(ShadersLoader.Find("PSX/BattleMap_SelectCursor_Abr_1"));
        meshRenderer.material = material;
        gameObject.SetActive(false);
    }

    public static void InitCommand(BTL_DATA btl)
    {
        FF9StateBattleSystem stateBattleSystem = FF9StateSystem.Battle.FF9Battle;
        if (stateBattleSystem.btl_phase == 2)
            btl.tar_mode = 1;
        btl.sel_mode = 0;
        btl.finger_disp = false;
        //int index1 = btl.bi.player == 0 ? 24 + (btl.bi.line_no - 4) * 3 : btl.bi.line_no * 6;
        //CMD_DATA cmdData = stateBattleSystem.cmd_buffer[index1];
        for (Int32 i = 0; i < 6; ++i)
        {
            btl.cmd[i] = new CMD_DATA { regist = btl };
            ClearCommand(btl.cmd[i]);
            ClearReflecData(btl.cmd[i]);
        }
    }

    public static void SetCommand(CMD_DATA cmd, BattleCommandId commandId, UInt32 sub_no, UInt16 tar_id, UInt32 cursor)
    {
        if (btl_cmd.CheckUsingCommand(cmd))
            return;
        BTL_DATA btl = cmd.regist;
        switch (commandId)
        {
            case BattleCommandId.SysEscape:
                if ((FF9StateSystem.Battle.FF9Battle.cmd_status & 1) != 0)
                {
                    cmd.sub_no = (Byte)sub_no;
                    return;
                }
                FF9StateSystem.Battle.FF9Battle.cmd_status |= 1;
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.aa_data[180]);
                break;
            case BattleCommandId.SysDead:
            case BattleCommandId.SysReraise:
            case BattleCommandId.SysStone:
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.aa_data[0]);
                break;
            case BattleCommandId.JumpAttack:
            case BattleCommandId.JumpTrance:
                FF9StateSystem.Battle.FF9Battle.cmd_status |= 16;
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.aa_data[sub_no]);
                break;
            case BattleCommandId.Item:
            case BattleCommandId.AutoPotion:
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.aa_data[0]);
                break;
            case BattleCommandId.Throw:
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.aa_data[190]);
                break;
            case BattleCommandId.MagicCounter:
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.enemy_attack[sub_no]);
                break;
            case BattleCommandId.Steal:
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.aa_data[sub_no]);
                break;
            default:
                cmd.SetAAData(FF9StateSystem.Battle.FF9Battle.aa_data[sub_no]);
                break;
        }
        // ScriptId 80: Double cast with AA specified by "Power" and "Rate"
        if (btl != null && btl.bi.player != 0 && cmd.ScriptId == 80)
        {
            AA_DATA first_aa = FF9StateSystem.Battle.FF9Battle.aa_data[cmd.Power];
            AA_DATA second_aa = FF9StateSystem.Battle.FF9Battle.aa_data[cmd.HitRate];
            UInt16 first_tar_id;
            UInt16 second_tar_id;
            if (first_aa.Info.Target == TargetType.AllAlly || first_aa.Info.Target == TargetType.RandomAlly)
            {
                first_tar_id = 0xF;
                second_tar_id = tar_id;
            }
            else if (first_aa.Info.Target == TargetType.AllEnemy || first_aa.Info.Target == TargetType.RandomEnemy)
            {
                first_tar_id = 0xF0;
                second_tar_id = tar_id;
            }
            else if (first_aa.Info.Target == TargetType.Everyone)
            {
                first_tar_id = 0xFF;
                second_tar_id = tar_id;
            }
            else if (first_aa.Info.Target == TargetType.Self)
            {
                first_tar_id = btl.btl_id;
                second_tar_id = tar_id;
            }
            else if (second_aa.Info.Target == TargetType.AllAlly || second_aa.Info.Target == TargetType.RandomAlly)
            {
                first_tar_id = tar_id;
                second_tar_id = 0xF;
            }
            else if (second_aa.Info.Target == TargetType.AllEnemy || second_aa.Info.Target == TargetType.RandomEnemy)
            {
                first_tar_id = tar_id;
                second_tar_id = 0xF0;
            }
            else if (second_aa.Info.Target == TargetType.Everyone)
            {
                first_tar_id = tar_id;
                second_tar_id = 0xFF;
            }
            else if (second_aa.Info.Target == TargetType.Self)
            {
                first_tar_id = tar_id;
                second_tar_id = btl.btl_id;
            }
            else
            {
                if (!first_aa.Info.DefaultAlly && (tar_id & 0xF0) == tar_id)
                {
                    first_tar_id = tar_id;
                    if (!second_aa.Info.DefaultAlly)
                        second_tar_id = 0xF0;
                    else
                        second_tar_id = 0xF;
                }
                else
                {
                    if (!first_aa.Info.DefaultAlly)
                        first_tar_id = 0xF0;
                    else
                        first_tar_id = 0xF;
                    second_tar_id = tar_id;
                }
            }
            UInt32 first_cursor = ((first_tar_id & 0xF0) == 0xF0) || ((first_tar_id & 0xF) == 0xF) ? 1u : 0u;
            UInt32 second_cursor = ((second_tar_id & 0xF0) == 0xF0) || ((second_tar_id & 0xF) == 0xF) ? 1u : 0u;
            CMD_DATA first_cmd = cmd.regist.cmd[3];
            CMD_DATA second_cmd = cmd.regist.cmd[0];
            first_cmd.regist.sel_mode = 1;
            first_cmd.info.CustomMPCost = cmd.aa.MP;
            second_cmd.info.CustomMPCost = 0;
            btl_cmd.SetCommand(first_cmd, commandId, cmd.Power, first_tar_id, first_cursor);
            btl_cmd.SetCommand(second_cmd, commandId, cmd.HitRate, second_tar_id, second_cursor);
            return;
        }

        cmd.tar_id = tar_id;
        cmd.cmd_no = commandId;
        cmd.sub_no = (Byte)sub_no;
        cmd.info.cursor = (Byte)cursor;
        cmd.info.cover = 0;
        cmd.info.dodge = 0;
        cmd.info.reflec = 0;
        cmd.IsShortRange = btl_cmd.IsAttackShortRange(cmd);
        if (commandId > BattleCommandId.BoundaryCheck)
        {
            cmd.info.priority = 1;
        }
        else
        {
            btl_stat.RemoveStatus(btl, BattleStatus.Defend);
            if (btl.bi.player != 0 && (commandId == BattleCommandId.SummonGarnet || commandId == BattleCommandId.Phantom || commandId == BattleCommandId.SummonEiko))
            {
                btl.summon_count++;
                if (Configuration.Battle.SummonPriorityCount < 0 || btl.summon_count <= Configuration.Battle.SummonPriorityCount)
                    cmd.info.priority = 1;
            }
        }
        if (commandId < BattleCommandId.EnemyReaction)
        {
            //if (btl_util.getCurCmdPtr() != btl.cmd[4])
            //{
            //    if (btl_mot.checkMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL))
            //    {
            //        btl_mot.setMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_NORMAL_TO_CMD);
            //        btl.evt.animFrame = 0;
            //    }
            //    else if (btl_mot.checkMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_DYING))
            //    {
            //        btl_mot.setMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_DYING_TO_CMD);
            //        btl.evt.animFrame = 0;
            //    }
            //    else if (btl_mot.checkMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_DEFENCE) && commandId < BattleCommandId.BoundaryCheck)
            //    {
            //        btl_mot.setMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_DEF_TO_IDLE);
            //        btl.evt.animFrame = 0;
            //    }
            //}
            btl.bi.cmd_idle = 1;
            if (Configuration.Battle.Speed < 3 && btl != null && btl.bi.player != 0)
                btl_mot.SetDefaultIdle(btl); // Don't wait for the "Stand" animation to finish its cycle to get ready
        }
        EnqueueCommand(cmd);
    }

    public static void SetCounter(BTL_DATA btl, BattleCommandId commandId, Int32 sub_no, UInt16 tar_id)
    {
        if (btl_stat.CheckStatus(btl, BattleStatus.Petrify | BattleStatus.Venom | BattleStatus.Stop | BattleStatus.Sleep | BattleStatus.Freeze | BattleStatus.Death) || FF9StateSystem.Battle.FF9Battle.btl_phase != 4)
            return;
        if (sub_no == 176 && btl.is_monster_transform)
		{
            if (btl.monster_transform.attack == 0)
                return;
            sub_no = (Int32)btl.monster_transform.attack;
        }
        SetCommand(btl.cmd[1], commandId, (UInt32)sub_no, tar_id, 0U);
    }

    public static Int16 GetPhantomCount(BattleUnit btl)
    {
        return (Int16)((60 - btl.Will) * 4 * 50);
    }

    public static void SetAutoCommand(BTL_DATA btl)
    {
        if (btl_stat.CheckStatus(btl, BattleStatus.Confuse))
            SetCommand(btl.cmd[0], BattleCommandId.Attack, 176U, btl_util.GetRandomBtlID((UInt32)(Comn.random8() & 1)), 0U);
        else if (btl_stat.CheckStatus(btl, BattleStatus.Berserk))
            SetCommand(btl.cmd[0], BattleCommandId.Attack, 176U, btl_util.GetRandomBtlID(0U), 0U);
    }

    public static void SetEnemyCommandBySequence(UInt16 tar_id, BattleCommandId cmd_no, UInt32 sub_no)
    {
        FF9StateBattleSystem stateBattleSystem = FF9StateSystem.Battle.FF9Battle;
        BattleUnit btl = btl_scrp.FindBattleUnit(16);
        BTL_DATA[] btlDataArray = stateBattleSystem.btl_data;
        SEQ_WORK_SET seqWorkSet = stateBattleSystem.seq_work_set;
        Int32 enemyType = Array.IndexOf(seqWorkSet.AnmOfsList.Distinct().ToArray(), seqWorkSet.AnmOfsList[sub_no]);
        for (Int32 index = 0; index < btlDataArray.Length; ++index)
        {
            if (enemyType == btlDataArray[index].typeNo)
            {
                btl = new BattleUnit(btlDataArray[index]);
                break;
            }
        }
        if (btl == null)
            return;
        if ((stateBattleSystem.cmd_status & 1) != 0 || btl.IsUnderStatus(BattleStatus.PreventEnemyCmd))
        {
            btl.Data.sel_mode = 0;
        }
        else
        {
            CMD_DATA cmd;
            if (cmd_no == BattleCommandId.EnemyAtk)
            {
                if (stateBattleSystem.btl_phase != 4)
                {
                    btl.Data.sel_mode = 0;
                    return;
                }
                cmd = btl.Data.cmd[0];
                if (btl.IsUnderStatus(BattleStatus.Confuse))
                {
                    tar_id = btl_util.GetRandomBtlID((UInt32)(Comn.random8() & 1));
                    sub_no = btl.EnemyType.p_atk_no;
                }
                else if (btl.IsUnderStatus(BattleStatus.Berserk))
                {
                    tar_id = btl_util.GetRandomBtlID(1U);
                    sub_no = btl.EnemyType.p_atk_no;
                }
            }
            else if (cmd_no == BattleCommandId.EnemyCounter)
                cmd = btl.Data.cmd[1];
            else if (cmd_no == BattleCommandId.EnemyDying)
                cmd = btl.Data.cmd[1];
		    else if (cmd_no == BattleCommandId.ScriptCounter1)
		    {
                cmd = btl.Data.cmd[3];
			    cmd_no = BattleCommandId.EnemyCounter;
		    }
		    else if (cmd_no == BattleCommandId.ScriptCounter2)
		    {
                cmd = btl.Data.cmd[4];
			    cmd_no = BattleCommandId.EnemyCounter;
		    }
		    else if (cmd_no == BattleCommandId.ScriptCounter3)
		    {
                cmd = btl.Data.cmd[5];
			    cmd_no = BattleCommandId.EnemyCounter;
		    }
            else
            {
                btl.Data.sel_mode = 0;
                return;
            }
            cmd.SetAAData(stateBattleSystem.enemy_attack[sub_no]);
            cmd.ScriptId = 26;
            cmd.tar_id = tar_id;
            cmd.cmd_no = cmd_no;
            cmd.sub_no = (Byte)sub_no;
            cmd.info.cursor = (Int32)cmd.aa.Info.Target < 6 || (Int32)cmd.aa.Info.Target >= 13 ? (Byte)0 : (Byte)1;
            cmd.IsShortRange = btl_cmd.IsAttackShortRange(cmd);
            if (cmd_no > BattleCommandId.BoundaryCheck)
            {
                cmd.info.priority = 1;
            }
            else
            {
                /*int num2 = (int)*/
                btl_stat.RemoveStatus(cmd.regist, BattleStatus.Defend);
            }
            cmd.info.cover = 0;
            cmd.info.dodge = 0;
            cmd.info.reflec = 0;
            btl.Data.bi.cmd_idle = 1;
            EnqueueCommand(cmd);
        }
    }

    public static void SetEnemyCommand(UInt16 own_id, UInt16 tar_id, BattleCommandId cmd_no, UInt32 sub_no)
    {
        FF9StateBattleSystem stateBattleSystem = FF9StateSystem.Battle.FF9Battle;
        BattleUnit btlDataPtr = btl_scrp.FindBattleUnit(own_id);
        if ((stateBattleSystem.cmd_status & 1) != 0 || btlDataPtr.IsUnderStatus(BattleStatus.PreventEnemyCmd))
        {
            btlDataPtr.Data.sel_mode = 0;
            return;
        }
        if (btl_para.IsNonDyingVanillaBoss(btlDataPtr.Data) && btlDataPtr.CurrentHp < 10000 && stateBattleSystem.btl_phase == 4 && btl_scrp.GetBattleID(1) == btlDataPtr.Id)
        {
            // Avoid bosses to keep attacking under 10000 HP in Speed modes >= 3 (because their AI script will not enter the ending phase if SFX keep playing)
            btlDataPtr.Data.sel_mode = 0;
            return;
        }
        CMD_DATA cmd;
        if (cmd_no == BattleCommandId.EnemyAtk)
        {
            if (stateBattleSystem.btl_phase != 4)
            {
                btlDataPtr.Data.sel_mode = 0;
                return;
            }
            cmd = btlDataPtr.Data.cmd[0];
            if (btlDataPtr.IsUnderStatus(BattleStatus.Confuse))
            {
                tar_id = btl_util.GetRandomBtlID((UInt32)(Comn.random8() & 1));
                sub_no = btlDataPtr.EnemyType.p_atk_no;
            }
            else if (btlDataPtr.IsUnderStatus( BattleStatus.Berserk))
            {
                tar_id = btl_util.GetRandomBtlID(1U);
                sub_no = btlDataPtr.EnemyType.p_atk_no;
            }
        }
        else if (cmd_no == BattleCommandId.EnemyCounter)
            cmd = btlDataPtr.Data.cmd[1];
        else if (cmd_no == BattleCommandId.EnemyDying)
            cmd = btlDataPtr.Data.cmd[1];
        else if (cmd_no == BattleCommandId.ScriptCounter1)
        {
            cmd = btlDataPtr.Data.cmd[3];
            cmd_no = BattleCommandId.EnemyCounter;
        }
        else if (cmd_no == BattleCommandId.ScriptCounter2)
        {
            cmd = btlDataPtr.Data.cmd[4];
            cmd_no = BattleCommandId.EnemyCounter;
        }
        else if (cmd_no == BattleCommandId.ScriptCounter3)
        {
            cmd = btlDataPtr.Data.cmd[5];
            cmd_no = BattleCommandId.EnemyCounter;
        }
        else
        {
            btlDataPtr.Data.sel_mode = 0;
            return;
        }
        if (btl_cmd.CheckUsingCommand(cmd))
        {
            if (cmd_no == BattleCommandId.EnemyAtk)
            {
                btlDataPtr.Data.sel_mode = 0;
                return;
            }
            else if (cmd_no == BattleCommandId.EnemyDying && cmd.cmd_no != BattleCommandId.EnemyDying)
            {
                while (btlDataPtr.Data.cmd.Count < 7) // Make sure that EnemyDying is taken into account, even by adding a new command to BTL_DATA
                {
                    cmd = new CMD_DATA { regist = btlDataPtr.Data };
                    btlDataPtr.Data.cmd.Add(cmd);
                    ClearCommand(cmd);
                    ClearReflecData(cmd);
                }
                cmd = btlDataPtr.Data.cmd[6];
            }
            else
            {
                return;
            }
        }
        cmd.SetAAData(stateBattleSystem.enemy_attack[sub_no]);
        cmd.tar_id = tar_id;
        cmd.cmd_no = cmd_no;
        cmd.sub_no = (Byte)sub_no;
        cmd.info.cursor = (Int32)cmd.aa.Info.Target < 6 || (Int32)cmd.aa.Info.Target >= 13 ? (Byte)0 : (Byte)1;
        cmd.IsShortRange = btl_cmd.IsAttackShortRange(cmd);
        if (cmd_no > BattleCommandId.BoundaryCheck)
        {
            cmd.info.priority = 1;
        }
        else
        {
            /*int num = (int)*/
            btl_stat.RemoveStatus(cmd.regist, BattleStatus.Defend);
        }
        cmd.info.cover = 0;
        cmd.info.dodge = 0;
        cmd.info.reflec = 0;
        btlDataPtr.Data.bi.cmd_idle = 1;
        EnqueueCommand(cmd);
    }

    private static void RunCommandFromQueue(FF9StateBattleSystem btlsys)
    {
        Boolean admitNewCommand = btlsys.cur_cmd == null;
        if (admitNewCommand)
            btl_cmd.next_cmd_delay = 0;
        if (!admitNewCommand && Configuration.Battle.Speed >= 3)
        {
            btl_cmd.next_cmd_delay--;
            if (btl_cmd.next_cmd_delay <= 0)
                admitNewCommand = true;
        }
        if (!admitNewCommand)
            return;
        if (btlsys.cmd_queue.next == null)
            return;
        if (btlsys.cur_cmd_list.Contains(btlsys.cmd_escape))
            return;
        if (Configuration.Battle.Speed < 3)
            for (BTL_DATA next = btlsys.btl_list.next; next != null; next = next.next)
                if (next.die_seq > 0 && next.die_seq < 6 && btl_stat.CheckStatus(next, BattleStatus.AutoLife))
                    return;

        CMD_DATA cmd = btlsys.cmd_queue.next;
        HashSet<BTL_DATA> busyCasters = new HashSet<BTL_DATA>();
        if (Configuration.Battle.Speed == 4)
            for (BTL_DATA next = btlsys.btl_list.next; next != null; next = next.next)
                if (btl_util.IsBtlBusy(next, btl_util.BusyMode.ANY_CURRENT))
                    busyCasters.Add(next);
        while (cmd != null)
        {
            if (cmd.regist != null && busyCasters.Contains(cmd.regist) && cmd.cmd_no != BattleCommandId.SysPhantom
                || cmd.cmd_no <= BattleCommandId.SysTrans && cmd.cmd_no != BattleCommandId.SysEscape && cmd.cmd_no != BattleCommandId.SysLastPhoenix && btl_stat.CheckStatus(cmd.regist, BattleStatus.Immobilized)
                || cmd.cmd_no == BattleCommandId.SysPhantom && Status.checkCurStat(cmd.regist, BattleStatus.Death)
                || (Configuration.Battle.Speed >= 4 && btl_util.IsBtlUsingCommandMotion(cmd.regist))
                || (Configuration.Battle.Speed >= 5 && cmd.regist.bi.cover != 0))
            {
                if (Configuration.Battle.Speed == 4)
                {
                    if (cmd.regist != null)
                        busyCasters.Add(cmd.regist);
                    foreach (BTL_DATA next in btl_util.findAllBtlData(cmd.tar_id))
                        busyCasters.Add(next);
                }
                cmd = cmd.next;
                continue;
            }
            break;
        }
        if (cmd == null || !FF9StateSystem.Battle.isDebug && !UIManager.Battle.IsNativeEnableAtb() && cmd.cmd_no < BattleCommandId.BoundaryCheck)
            return;
        if (Configuration.Battle.Speed == 3 && cmd.regist != null && btl_util.IsBtlBusy(cmd.regist, btl_util.BusyMode.ANY_CURRENT))
            return;
        if (cmd.cmd_no == BattleCommandId.SysEscape && btlsys.cur_cmd_list.Count > 0)
            return;

        btl_cmd.next_cmd_delay = btl_cmd.cmd_delay_max;
        if (cmd.cmd_no < BattleCommandId.EnemyReaction)
        {
            BTL_DATA btl = cmd.regist;
            //if (cmd.cmd_no > BattleCommandId.BoundaryCheck && cmd.cmd_no < BattleCommandId.EnemyCounter)
            //{
            //    btl_mot.setMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD);
            //    btl.evt.animFrame = 0;
            //}
            //if (btl.bi.player != 0 && !btl_mot.checkMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD) && !btl_mot.checkMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_ESCAPE) && !Status.checkCurStat(btl, BattleStatus.Jump))
            //{
            //    if (!btl_mot.checkMotion(btl, btl.bi.def_idle) || btl.bi.cmd_idle != 0)
            //        return;
            //    btl_mot.setMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD);
            //    btl.evt.animFrame = 0;
            //    return;
            //}
            if (btl_stat.CheckStatus(btl, BattleStatus.Heat))
            {
                /*int num = (int)*/
                btl_stat.AlterStatus(btl, BattleStatus.Death);
                return;
            }
        }
        btlsys.cur_cmd_list.Add(cmd);
        KillCommand(cmd);
    }

    public static void CommandEngine(FF9StateBattleSystem btlsys)
    {
        btl_cmd.RunCommandFromQueue(btlsys);
        List<CMD_DATA> curCommandDuplicate = new List<CMD_DATA>();
        foreach (CMD_DATA cmd in btlsys.cur_cmd_list)
            curCommandDuplicate.Add(cmd);
        foreach (CMD_DATA cmd in curCommandDuplicate)
        {
            BTL_DATA caster = cmd.regist;
            switch (cmd.info.mode)
            {
                case command_mode_index.CMD_MODE_INSPECTION:
                    if (!CheckCommandCondition(btlsys, cmd) || !CheckTargetCondition(btlsys, cmd) || !CheckMpCondition(cmd))
                    {
                        ResetItemCount(cmd);
                        //if (caster != null && caster.bi.player != 0 && btl_mot.checkMotion(caster, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD))
                        //    btl_mot.setMotion(caster, BattlePlayerCharacter.PlayerMotionIndex.MP_CMD_TO_NORMAL);
                        cmd.info.mode = command_mode_index.CMD_MODE_IDLE;
                        break;
                    }
                    //if (caster != null && !btl_mot.checkMotion(caster, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD) && cmd1.cmd_no < BattleCommandId.EnemyAtk)
                    //{
                    //    btl_mot.setMotion(caster, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD);
                    //    caster.evt.animFrame = 0;
                    //}
                    if (caster != null && caster.bi.player != 0)
                    {
                        if (cmd.cmd_no == BattleCommandId.Throw)
                            FF9StateSystem.EventState.IncreaseAAUsageCounter(190);
                        else if (cmd.cmd_no != BattleCommandId.Item && cmd.cmd_no != BattleCommandId.AutoPotion && cmd.cmd_no != BattleCommandId.MagicCounter &&
                                 cmd.cmd_no != BattleCommandId.SysDead && cmd.cmd_no != BattleCommandId.SysReraise && cmd.cmd_no != BattleCommandId.SysStone)
                            FF9StateSystem.EventState.IncreaseAAUsageCounter(cmd.sub_no);
                    }
                    cmd.info.mode = command_mode_index.CMD_MODE_SELECT_VFX;
                    break;
                case command_mode_index.CMD_MODE_SELECT_VFX:
                {
                    if (cmd.regist != null && cmd.regist.bi.player != 0 && cmd.cmd_no <= BattleCommandId.RushAttack)
                    {
                        BattlePlayerCharacter.PlayerMotionIndex motion = btl_mot.getMotion(cmd.regist);
                        BattlePlayerCharacter.PlayerMotionStance stance = btl_mot.EndingMotionStance(motion);
                        if (stance == BattlePlayerCharacter.PlayerMotionStance.NORMAL || stance == BattlePlayerCharacter.PlayerMotionStance.DYING || stance == BattlePlayerCharacter.PlayerMotionStance.DEFEND || stance == BattlePlayerCharacter.PlayerMotionStance.DISABLE)
                            break;
                        if (stance == BattlePlayerCharacter.PlayerMotionStance.CMD && motion != BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD && cmd.regist.animEndFrame)
                            break;
                    }
                    Boolean cancel = true;
                    foreach (BTL_DATA btl in btl_util.findAllBtlData(cmd.tar_id))
                        if (btl.bi.target != 0 /*&& btl.bi.disappear == 0*/)
                        {
                            cancel = false;
                            break;
                        }
                    if (cancel)
                    {
                        ResetItemCount(cmd);
                        cmd.info.mode = command_mode_index.CMD_MODE_DONE;
                        break;
                    }
                    UInt16 tryCover = 0;
                    for (BTL_DATA next = btlsys.btl_list.next; next != null; next = next.next)
                        foreach (SupportingAbilityFeature saFeature in ff9abil.GetEnabledSA(next.sa))
                            saFeature.TriggerOnCommand(new BattleUnit(next), new BattleCommand(cmd), ref tryCover);
                    if (tryCover != 0)
                    {
                        foreach (BTL_DATA target in btl_util.findAllBtlData(cmd.tar_id))
                        {
                            UInt16 coverId = btl_abil.CheckCoverAbility(target, tryCover);
                            if (coverId != 0)
                            {
                                cmd.tar_id &= (UInt16)~target.btl_id;
                                cmd.tar_id |= coverId;
                                cmd.info.cover = 1;
                            }
                        }
                    }
                    btl_vfx.SelectCommandVfx(cmd);
                    cmd.info.mode = command_mode_index.CMD_MODE_LOOP;
                    break;
                }
                case command_mode_index.CMD_MODE_LOOP:
                    if (Configuration.Battle.SFXRework || caster.bi.player != 0 || cmd.info.mon_reflec != 0)
                        CheckCommandLoop(cmd);
                    break;
                case command_mode_index.CMD_MODE_IDLE:
                    if (cmd.cmd_no != BattleCommandId.SysEscape)
                    {
                        ReqFinishCommand(cmd);
                        break;
                    }
                    FinishCommand(btlsys, cmd);
                    break;
            }
            if (cmd.info.mode == command_mode_index.CMD_MODE_DONE)
                FinishCommand(btlsys, cmd);
        }
    }

    public static void KillCommand(CMD_DATA cmd)
    {
        BTL_DATA btlData = cmd.regist;
        for (CMD_DATA cmd1 = FF9StateSystem.Battle.FF9Battle.cmd_queue; cmd1 != null; cmd1 = cmd1.next)
        {
            if (cmd1.next == cmd)
            {
                if (btlData != null && (btlData.bi.player == 0 || (cmd != btlData.cmd[3] && cmd != btlData.cmd[4])))
                    btlData.bi.cmd_idle = 0;
                DequeueCommand(cmd1, false);
                break;
            }
        }
    }

    public static Boolean KillCommand2(BTL_DATA btl)
    {
        Boolean flag = false;
        btl.bi.cmd_idle = 0;
        CMD_DATA cmd1 = FF9StateSystem.Battle.FF9Battle.cmd_queue;
        while (cmd1 != null)
        {
            CMD_DATA cmd2 = cmd1.next;
            if (cmd2 != null && cmd2.regist == btl && cmd2.cmd_no < BattleCommandId.EnemyDying)
            {
                if (cmd2.cmd_no < BattleCommandId.BoundaryCheck)
                    flag = true;
                ResetItemCount(cmd2);
                DequeueCommand(cmd1, true);
            }
            else
                cmd1 = cmd2;
        }
        return flag;
    }

    private static void ResetItemCount(CMD_DATA cmd)
    {
        if (cmd.cmd_no != BattleCommandId.Item && cmd.cmd_no != BattleCommandId.Throw && cmd.cmd_no != BattleCommandId.AutoPotion)
            return;
        UIManager.Battle.ItemUnuse(cmd.sub_no);
    }

    private static void DequeueCommand(CMD_DATA cmd, Boolean escape_check)
    {
        CMD_DATA cmdData = cmd.next;
        cmd.next = cmdData.next;
        cmdData.info.stat = 0;
        cmdData.info.priority = 0;
        if (escape_check && cmdData.cmd_no == BattleCommandId.SysEscape)
            FF9StateSystem.Battle.FF9Battle.cmd_status &= 65534;
    }

    public static Boolean CheckSpecificCommand(BTL_DATA btl, BattleCommandId cmd_no)
    {
        for (CMD_DATA cmdData = FF9StateSystem.Battle.FF9Battle.cmd_queue; cmdData != null; cmdData = cmdData.next)
            if (cmdData.regist == btl && cmdData.cmd_no == cmd_no)
                return true;
        foreach (CMD_DATA cmdData in FF9StateSystem.Battle.FF9Battle.cur_cmd_list)
            if (cmdData.regist == btl && cmdData.cmd_no == cmd_no)
                return true;
        return false;
    }

    public static Boolean CheckSpecificCommand2(BattleCommandId cmd_no)
    {
        for (CMD_DATA cmdData = FF9StateSystem.Battle.FF9Battle.cmd_queue; cmdData != null; cmdData = cmdData.next)
            if (cmdData.cmd_no == cmd_no)
                return true;
        foreach (CMD_DATA cmdData in FF9StateSystem.Battle.FF9Battle.cur_cmd_list)
            if (cmdData.cmd_no == cmd_no)
                return true;
        return false;
    }

    public static Boolean CheckUsingCommand(CMD_DATA cmd)
    {
        for (CMD_DATA cmdData = FF9StateSystem.Battle.FF9Battle.cmd_queue; cmdData != null; cmdData = cmdData.next)
            if (cmd == cmdData)
                return true;
        foreach (CMD_DATA cmdData in FF9StateSystem.Battle.FF9Battle.cur_cmd_list)
            if (cmdData == cmd)
                return true;
        return false;
    }

    public static void EnqueueCommand(CMD_DATA cmd)
    {
        for (CMD_DATA cp = FF9StateSystem.Battle.FF9Battle.cmd_queue; cp != null; cp = cp.next)
        {
            if (cmd.info.priority != 0 && cp.next != null)
            {
                if (cmd.cmd_no > BattleCommandId.SysTrans || cmd.cmd_no == BattleCommandId.EnemyDying)
                {
                    if (cp.next.cmd_no < BattleCommandId.SysDead && cp.next.cmd_no != BattleCommandId.EnemyDying)
                    {
                        InsertCommand(cmd, cp);
                        break;
                    }
                }
                else if (cmd.cmd_no == BattleCommandId.SysTrans)
                {
                    if (cp.next.cmd_no < BattleCommandId.SysTrans && cp.next.cmd_no != BattleCommandId.EnemyDying)
                    {
                        InsertCommand(cmd, cp);
                        break;
                    }
                }
                else if (cp.next.info.priority == 0 || cp.next.cmd_no == BattleCommandId.SummonGarnet || cp.next.cmd_no == BattleCommandId.Phantom || cp.next.cmd_no == BattleCommandId.SummonEiko)
                {
                    InsertCommand(cmd, cp);
                    break;
                }
            }
            else if (cp.next == null)
            {
                InsertCommand(cmd, cp);
                break;
            }
        }
    }

    public static void InsertCommand(CMD_DATA cmd, CMD_DATA cp)
    {
        if (cmd == cp)
            return;
        cmd.next = cp.next;
        cp.next = cmd;
        cmd.info.stat = 1;
    }

    public static UInt16 CheckReflec(CMD_DATA cmd)
    {
        UInt16 reflectingBtl = 0;
        if (cmd.cmd_no != BattleCommandId.Item && cmd.cmd_no != BattleCommandId.Throw && cmd.cmd_no != BattleCommandId.AutoPotion && (cmd.AbilityCategory & 1) != 0 && !cmd.info.ReflectNull && !cmd.info.HasCheckedReflect)
        {
            UInt16[] targetablePlayers = new UInt16[4];
            UInt16[] targetableEnemies = new UInt16[4];
            Int16 enemyReflectCount = 0;
            Int16 partyReflectCount = 0;
            Int16 tpi = 0;
            Int16 tei = 0;
            for (BTL_DATA btl = FF9StateSystem.Battle.FF9Battle.btl_list.next; btl != null; btl = btl.next)
            {
                if ((btl.btl_id & cmd.tar_id) != 0)
                {
                    if (btl.bi.target != 0 && !btl_stat.CheckStatus(btl, BattleStatus.Petrify) && btl_stat.CheckStatus(btl, BattleStatus.Reflect))
                    {
                        reflectingBtl |= btl.btl_id;
                        if (btl.bi.player == 1)
                            partyReflectCount++;
                        else
                            enemyReflectCount++;
                    }
                }
                else if (!Status.checkCurStat(btl, BattleStatus.Death) && btl.bi.target != 0)
                {
                    if (btl.bi.player == 1)
                        targetablePlayers[tpi++] = btl.btl_id;
                    else
                        targetableEnemies[tei++] = btl.btl_id;
                }
            }
            if (partyReflectCount != 0 || enemyReflectCount != 0)
            {
                List<UInt16> reflectList = new List<UInt16>();
                if (tei > 0)
                    for (Int32 i = 0; i < partyReflectCount; i++)
                        reflectList.Add(targetableEnemies[Comn.random8() % tei]);
                if (tpi > 0)
                    for (Int32 i = 0; i < enemyReflectCount; i++)
                        reflectList.Add(targetablePlayers[Comn.random8() % tpi]);
                for (Int32 index = 0; index < 4; ++index) // TODO: have more possible reflects at least with SFXRework system?
                    cmd.reflec.tar_id[index] = index >= reflectList.Count ? (UInt16)0 : reflectList[index];
                if (reflectingBtl == cmd.tar_id)
                {
                    cmd.info.reflec = 1;
                    cmd.tar_id = MergeReflecTargetID(cmd.reflec);
                }
                else
                {
                    cmd.info.reflec = 2;
                    cmd.tar_id = (UInt16)(cmd.tar_id & ~reflectingBtl);
                }
            }
            cmd.info.HasCheckedReflect = true;
        }
        return reflectingBtl;
    }

    public static void KillCommand3(BTL_DATA btl)
    {
        CMD_DATA cmd1 = FF9StateSystem.Battle.FF9Battle.cmd_queue;
        while (cmd1 != null)
        {
            CMD_DATA cmd2 = cmd1.next;
            if (cmd2 != null && cmd2.regist == btl)
            {
                ResetItemCount(cmd2);
                DequeueCommand(cmd1, true);
            }
            else
                cmd1 = cmd2;
        }
    }

    public static void KillSpecificCommand(BTL_DATA btl, BattleCommandId cmd_no)
    {
        for (CMD_DATA cmd = FF9StateSystem.Battle.FF9Battle.cmd_queue; cmd != null; cmd = cmd.next)
        {
            if (cmd.next != null && cmd.next.regist == btl && cmd.next.cmd_no == cmd_no)
            {
                DequeueCommand(cmd, true);
                break;
            }
        }
    }

    // ReSharper disable PossibleNullReferenceException
    public static Boolean CheckCommandCondition(FF9StateBattleSystem btlsys, CMD_DATA cmd)
    {
        BattleUnit caster = cmd.regist == null ? null : new BattleUnit(cmd.regist);
        FF9StateGlobal ff9StateGlobal = FF9StateSystem.Common.FF9;
        if (caster != null)
        {
            if (caster.IsUnderStatus(BattleStatus.Death) && cmd.cmd_no <= BattleCommandId.EnemyCounter)
                return false;
            if (cmd.cmd_no < BattleCommandId.EnemyReaction)
                caster.FaceTheEnemy();

            // Garnet is depressed.
            if (battle.GARNET_DEPRESS_FLAG != 0 && caster.IsPlayer && caster.PlayerIndex == CharacterIndex.Garnet && !Configuration.Battle.GarnetConcentrate)
            {
                if (cmd.cmd_no < BattleCommandId.BoundaryCheck && Comn.random8() < 64) // 25%
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.DaggerCannotConcentrate);
                    return false;
                }
            }

            if (!CheckMagicCondition(cmd))
                return false;

            /*if (cmd.IsShortRange && Comn.countBits(cmd.tar_id) > 1)
            {
                for (BTL_DATA next = btlsys.btl_list.next; next != null; next = next.next)
                    if (next.out_of_reach && (next.btl_id & cmd.tar_id) != 0)
                        cmd.tar_id &= (UInt16)~next.btl_id;
                if (cmd.tar_id == 0)
                {
                    UIManager.Battle.SetBattleFollowMessage(23, new object[0]);
                    return false;
                }
            }*/
        }

        // In SFXRework mode, the random target pick (for player characters) is implemented in ef237/PlayerSequence.seq instead
        if (!Configuration.Battle.SFXRework && (SpecialEffect)cmd.aa.Info.VfxIndex == SpecialEffect.Roulette)
            cmd.tar_id = btl_util.GetRandomBtlID((UInt32)(Comn.random8() & 1));

        BattleCommandId commandId = cmd.cmd_no;
        switch (commandId)
        {
            case BattleCommandId.Attack:
                /* Check removed: "CannotReach" is checked in CheckTargetCondition
                if (btlsys.btl_scene.Info.NoNeighboring != 0 && caster.HasCategory(WeaponCategory.ShortRange) && cmd.tar_id > 15)
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CannotReach);
                    return false;
                }*/
                break;
            case BattleCommandId.JumpTrance:
                caster.Data.cmd[3].tar_id = btl_util.GetStatusBtlID(1U, 0U);
                break;
            case BattleCommandId.Jump:
            case BattleCommandId.Jump2:
                caster.AlterStatus(BattleStatus.Jump);
                caster.Data.cmd[3].cmd_no = cmd.cmd_no;
                caster.Data.cmd[3].tar_id = cmd.cmd_no != BattleCommandId.Jump ? btl_util.GetStatusBtlID(1U, 0U) : cmd.tar_id;
                cmd.tar_id = caster.Id;
                break;
            case BattleCommandId.Item:
            case BattleCommandId.AutoPotion:
            case BattleCommandId.Throw:
                if (ff9item.FF9Item_GetCount(cmd.sub_no) == 0)
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.NotEnoughItems);
                    return false;
                }
                if (cmd.cmd_no == BattleCommandId.AutoPotion)
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.AutoPotion);
                break;
            case BattleCommandId.SummonGarnet:
            case BattleCommandId.SummonEiko:
            case BattleCommandId.Phantom:
                DecideSummonType(cmd);
                break;
            case BattleCommandId.BlackMagic:
            case BattleCommandId.DoubleBlackMagic:
                if (cmd.sub_no == 46)
                    DecideMeteor(cmd);
                break;
            case BattleCommandId.BlueMagic:
                if (cmd.sub_no == 93 && ff9item.FF9Item_GetCount(cmd.Power) < btl_util.SumOfTarget(1U))
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.NotEnoughItems);
                    return false;
                }
                break;
            case BattleCommandId.MasterTrick:
            case BattleCommandId.SuperTrick:
                if (cmd.sub_no == 126 || cmd.sub_no == 134)
                    if (!SpendSpareChangeGil(caster, cmd))
                        UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.NotEnoughGil);
                if (cmd.sub_no == 129 || cmd.sub_no == 137)
                {
                    cmd.Element = (EffectElement)(1 << Comn.random8() % 8);
                    if (Configuration.Battle.CurseUseWeaponElement && caster.Data.weapon.Ref.Elements != 0)
                        cmd.Element = (EffectElement)Comn.randomID(caster.Data.weapon.Ref.Elements);
                    cmd.ElementForBonus = cmd.Element;
                }
                break;
            case BattleCommandId.MagicSword:
                return DecideMagicSword(caster, cmd.aa.MP);
            case BattleCommandId.Counter:
                /*if (Configuration.Battle.CountersBetterTarget)
                {
                    int valid_target_count = 0;
                    for (BTL_DATA next = btlsys.btl_list.next; next != null; next = next.next)
                        if ((cmd.tar_id & next.btl_id) != 0 && next.bi.target != 0 && !Status.checkCurStat(next, BattleStatus.Death))
                            valid_target_count++;
                    if (valid_target_count == 0)
                        return false;
                }
                else
                {
                    BattleUnit btlDataPtr1;
                    if ((btlDataPtr1 = btl_scrp.FindBattleUnit(cmd.tar_id)) == null || btlDataPtr1.Data.bi.target == 0 || btlDataPtr1.IsUnderStatus(BattleStatus.Death))
                        return false;
                }
                if (btlsys.btl_scene.Info.NoNeighboring != 0 && caster.HasCategory(WeaponCategory.ShortRange))
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CannotReach);
                    return false;
                }*/
                if (cmd.sub_no == 176)
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CounterAttack);
                break;
            case BattleCommandId.MagicCounter:
                /*if (Configuration.Battle.CountersBetterTarget)
                {
                    int valid_target_count = 0;
                    for (BTL_DATA next = btlsys.btl_list.next; next != null; next = next.next)
                        if ((cmd.tar_id & next.btl_id) != 0 && next.bi.target != 0 && !Status.checkCurStat(next, BattleStatus.Death))
                            valid_target_count++;
                    if (valid_target_count == 0)
                        return false;
                }
                else
                {
                    BattleUnit btlDataPtr2;
                    if ((btlDataPtr2 = btl_scrp.FindBattleUnit(cmd.tar_id)) == null || btlDataPtr2.Data.bi.target == 0 || btlDataPtr2.IsUnderStatus(BattleStatus.Death))
                        return false;
                }*/
                UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.ReturnMagic);
                break;
            case BattleCommandId.RushAttack:
                /*if (btlsys.btl_scene.Info.NoNeighboring != 0 && caster.HasCategory(WeaponCategory.ShortRange))
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CannotReach);
                    return false;
                }
                if (cmd.cmd_no == BattleCommandId.Counter)
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CounterAttack);
                }*/
                break;
            case BattleCommandId.SysEscape:
                if (btlsys.btl_phase == 4)
                {
                    for (BTL_DATA btl = btlsys.btl_list.next; btl != null; btl = btl.next)
                    {
                        if (btl.bi.player != 0 && !btl_stat.CheckStatus(btl, BattleStatus.CannotEscape) && btl.cur.hp > 0)
                        {
                            if (!btl_mot.checkMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_ESCAPE))
                                btl_mot.SetDefaultIdle(btl, false);
                            //{
                            //    btl_mot.setMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_ESCAPE);
                            //    btl.evt.animFrame = 0;
                            //}
                            btlsys.btl_phase = 5;
                            btlsys.btl_seq = 3;
                        }
                    }
                    if (btlsys.btl_phase == 5 && btlsys.btl_seq == 3)
                    {
                        UIManager.Battle.SetIdle();
                        ++ff9StateGlobal.party.escape_no;
                        if (cmd.sub_no == 0)
                            ff9StateGlobal.btl_flag |= 4;
                        KillAllCommand(btlsys);
                    }
                    else
                        btlsys.cmd_status &= 65534;
                    return false;
                }
                break;
            case BattleCommandId.SysPhantom:
                if (cmd.sub_no != btlsys.phantom_no)
                {
                    cmd.sub_no = btlsys.phantom_no;
                    cmd.SetAAData(btlsys.aa_data[cmd.sub_no]);
                    cmd.IsShortRange = btl_cmd.IsAttackShortRange(cmd);
                }
                break;
            case BattleCommandId.SysTrans:
                if (caster.IsUnderStatus(BattleStatus.Trance))
                {
                    UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.Trance);
                    caster.Data.dms_geo_id = btl_init.GetModelID(btl_util.getSerialNumber(caster.Data) + 19);
                }
                else
                {
                    caster.Data.dms_geo_id = btl_init.GetModelID(btl_util.getSerialNumber(caster.Data));
                }
                return true;
            case BattleCommandId.SysDead: // Unused anymore
                btl_sys.CheckBattleMenuOff(caster);
                if (caster.Data.die_seq == 0)
                {
                    if (caster.IsPlayer)
                        caster.Data.die_seq = !btl_mot.checkMotion(caster.Data, BattlePlayerCharacter.PlayerMotionIndex.MP_DISABLE) ? (Byte)1 : (Byte)5;
                    else
                        caster.Data.die_seq = caster.IsSlave || caster.Data.bi.death_f == 0 ? (Byte)1 : (Byte)3;
                }
                return false;
            case BattleCommandId.SysReraise: // Unused anymore
                caster.CurrentHp = 1;
                /*int num4 = (int)*/
                caster.RemoveStatus(BattleStatus.Death);
                //caster.Data.bi.dmg_mot_f = 1;
                FF9StateSystem.Settings.SetHPFull();
                return false;
            case BattleCommandId.SysStone:
                btl_stat.StatusCommandCancel(caster.Data, BattleStatus.Petrify);
                caster.Data.stat.cur |= BattleStatus.Petrify;
                caster.CurrentAtb = 0;
                btl_sys.CheckBattlePhase(caster.Data);
                caster.RemoveStatus((BattleStatus)2147483648U);
                btl_stat.SetStatusClut(caster.Data, true);
                return false;
        }
        return true;
    }

    // ReSharper restore PossibleNullReferenceException
    public static Boolean CheckMagicCondition(CMD_DATA cmd)
    {
        if (!btl_stat.CheckStatus(cmd.regist, BattleStatus.Silence) || (cmd.AbilityCategory & 2) == 0)
            return true;
        UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CannotCast);
        return false;
    }

    public static void KillNormalCommand(FF9StateBattleSystem btlsys)
    {
        CMD_DATA cp = btlsys.cmd_queue;
        while (cp != null)
        {
            CMD_DATA ncp = cp.next;
            if (ncp != null && ncp.cmd_no != BattleCommandId.EnemyDying && ncp.cmd_no < BattleCommandId.SysLastPhoenix)
                ManageDequeueCommand(cp, ncp);
            else
                cp = ncp;
        }
    }

    public static void KillAllCommand(FF9StateBattleSystem btlsys)
    {
        CMD_DATA cp = btlsys.cmd_queue;
        while (cp != null)
        {
            CMD_DATA ncp = cp.next;
            if (ncp != null && ncp.cmd_no < BattleCommandId.SysTrans)
                ManageDequeueCommand(cp, ncp);
            else
                cp = ncp;
        }
        btlsys.cmd_status &= 65523;
        btlsys.phantom_no = (Byte)(btlsys.phantom_cnt = 0);
    }

    public static void ClearSysPhantom(BTL_DATA btl)
    {
        if (btl.bi.player == 0 || btl.bi.slot_no != 2)
            return;
        FF9StateBattleSystem stateBattleSystem = FF9StateSystem.Battle.FF9Battle;
        KillSpecificCommand(btl, BattleCommandId.SysPhantom);
        stateBattleSystem.cmd_status &= 65523;
        stateBattleSystem.phantom_no = 0;
        stateBattleSystem.phantom_cnt = 0;
    }

    public static void ManageDequeueCommand(CMD_DATA cp, CMD_DATA ncp)
    {
        if (cp == null)
            return;
        //FF9StateBattleSystem stateBattleSystem = FF9StateSystem.Battle.FF9Battle;
        BTL_DATA btl = ncp.regist;
        if (ncp.cmd_no < BattleCommandId.BoundaryCheck && btl != null && !Status.checkCurStat(btl, BattleStatus.Death))
        {
            btl.sel_mode = 0;
            //if (btl_mot.checkMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD)
            //    && (btl.bi.slot_no != 6 || stateBattleSystem.cur_cmd != null && stateBattleSystem.cur_cmd.cmd_no != BattleCommandId.DoubleWhiteMagic)
            //    && (btl.bi.slot_no != 1 || stateBattleSystem.cur_cmd != null && stateBattleSystem.cur_cmd.cmd_no != BattleCommandId.DoubleBlackMagic)/*&& stateBattleSystem.cur_cmd.cmd_no != BattleCommandId.MagicSword*/)
            {
                //btl_mot.setMotion(btl, BattlePlayerCharacter.PlayerMotionIndex.MP_CMD_TO_NORMAL);
                //btl.evt.animFrame = 0;
                btl.bi.cmd_idle = 0;
            }
        }
        ResetItemCount(ncp);
        DequeueCommand(cp, true);
    }

    private static Boolean CheckMpCondition(CMD_DATA cmd)
    {
        Int32 mp = cmd.info.CustomMPCost >= 0 ? cmd.info.CustomMPCost : cmd.aa.MP;
        if (cmd.info.IsZeroMP)
            mp = 0;
        
        if (battle.GARNET_SUMMON_FLAG != 0 && (cmd.AbilityType & 4) != 0)
            mp *= 4;

        if (cmd.regist != null)
        {
            if (cmd.regist.bi.player != 0)
                mp = mp * FF9StateSystem.Common.FF9.player[cmd.regist.bi.slot_no].mpCostFactor / 100;
            if (cmd.cmd_no == BattleCommandId.MagicCounter || ConsumeMp(cmd.regist, mp))
                return true;
        }
        
        UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.NotEnoughMp);
        return false;
    }

    private static Boolean CheckTargetCondition(FF9StateBattleSystem btlsys, CMD_DATA cmd)
    {
        UInt16 validTarId = 0;
        if (cmd.tar_id == 0)
            return false;
        Boolean forDead;
        switch (cmd.cmd_no)
        {
            case BattleCommandId.Item:
            case BattleCommandId.AutoPotion:
                forDead = ff9item._FF9Item_Info[btl_util.btlItemNum(cmd.sub_no)].info.ForDead;
                break;
            case BattleCommandId.SysTrans:
                return true;
            default:
                forDead = cmd.aa.Info.ForDead;
                break;
        }
        for (BTL_DATA btl = btlsys.btl_list.next; btl != null; btl = btl.next)
            if (btl.bi.target != 0 && (btl.btl_id & cmd.tar_id) != 0 && (forDead && btl.bi.player != 0 || !Status.checkCurStat(btl, BattleStatus.Death)) && (!btl.out_of_reach || !cmd.IsShortRange))
                validTarId |= btl.btl_id;
        if (validTarId != 0)
        {
            cmd.tar_id = validTarId;
            return true;
        }
        if (cmd.info.cursor == 0)
        {
            if (cmd.IsShortRange)
            {
                UInt16 targetInRange = 0;
                Boolean allowPlayer = (cmd.tar_id & 0xF) != 0;
                Boolean allowEnemy = (cmd.tar_id & 0xF0) != 0;
                for (BTL_DATA next = btlsys.btl_list.next; next != null; next = next.next)
                    if (((allowEnemy && next.bi.player == 0) || (allowPlayer && next.bi.player == 1)) && next.bi.target != 0 && !next.out_of_reach && (!Status.checkCurStat(next, BattleStatus.Death) || forDead))
                        targetInRange |= next.btl_id;
                cmd.tar_id = (UInt16)Comn.randomID(targetInRange);
                if (cmd.tar_id == 0)
                {
                    if (btl_util.GetRandomBtlID(allowPlayer ? 1U : 0U, forDead) != 0)
                        UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CannotReach);
                    return false;
                }
            }
            else
            {
                cmd.tar_id = btl_util.GetRandomBtlID(cmd.tar_id & 15U, forDead);
            }
            if (cmd.tar_id != 0)
                return true;
        }
        return false;
    }

    public static void ReqFinishCommand(CMD_DATA cmd)
    {
        if (cmd.info.reflec == 2 && !Configuration.Battle.SFXRework)
        {
            btl_vfx.LoopBattleVfxForReflect(cmd, (UInt32)cmd.aa.Info.VfxIndex);
            cmd.info.mode = command_mode_index.CMD_MODE_LOOP;
        }
        else
        {
            if (btl_stat.CheckStatus(cmd.regist, BattleStatus.AutoLife) && cmd.cmd_no == BattleCommandId.SysDead && !btl_mot.checkMotion(cmd.regist, BattlePlayerCharacter.PlayerMotionIndex.MP_DISABLE))
                return;
            cmd.info.mode = command_mode_index.CMD_MODE_DONE;
        }
    }

    public static void FinishCommand(FF9StateBattleSystem btlsys, CMD_DATA cmd)
    {
        BattleUnit caster = cmd.regist == null ? null : new BattleUnit(cmd.regist);
        BattleCommandId commandId = cmd.cmd_no;
        if (cmd.cmd_no < BattleCommandId.BoundaryCheck && caster != null)
        {
            if (cmd == caster.Data.cmd[0] && cmd.cmd_no != BattleCommandId.Jump && cmd.cmd_no != BattleCommandId.Jump2)
            {
                ResetCurrentBattlerActiveTime(caster);
            }
            else if (commandId == BattleCommandId.DoubleWhiteMagic && caster.PlayerIndex == CharacterIndex.Eiko && cmd == caster.Data.cmd[3] && !CheckUsingCommand(caster.Data.cmd[0]))
            {
                ResetCurrentBattlerActiveTime(caster);
            }
            else if (commandId == BattleCommandId.JumpAttack)
            {
                caster.RemoveStatus(BattleStatus.Jump);
                FF9StateSystem.Battle.FF9Battle.cmd_status &= 65519;
            }
            else if (commandId == BattleCommandId.JumpTrance)
            {
                caster.AlterStatus(BattleStatus.Jump);
                caster.Data.tar_mode = 2;
                caster.Data.SetDisappear(true, 2);
                FF9StateSystem.Battle.FF9Battle.cmd_status &= 65519;
            }

            if (IsNeedToDecreaseTrance(caster, commandId, cmd))
            {
                Byte tranceDelta = (Byte)((300 - caster.Level) / caster.Will * 10);

                if (btl_cmd.half_trance_cmd_list.Contains(cmd.cmd_no))
                    tranceDelta /= 2;

                if (FF9StateSystem.Settings.IsTranceFull)
                    tranceDelta = 0;

                if (caster.Trance > tranceDelta)
                    caster.Trance -= tranceDelta;
                else if (!FF9StateSystem.Battle.isDebug)
                    caster.RemoveStatus(BattleStatus.Trance);

                if (cmd.cmd_no == BattleCommandId.Phantom && btlsys.phantom_no != 0)
                {
                    btlsys.cmd_status |= 4;
                    btlsys.phantom_cnt = GetPhantomCount(caster);
                }
            }
        }
        else if (cmd.cmd_no == BattleCommandId.SysPhantom)
        {
            btlsys.cmd_status &= 65527;
            btlsys.phantom_cnt = GetPhantomCount(caster);
        }
        else if (cmd.cmd_no < BattleCommandId.EnemyCounter && CheckUsingCommand(caster.Data.cmd[0]))
        {
            caster.Data.bi.cmd_idle = 1;
        }

        if (cmd.info.cover != 0)
            foreach (BTL_DATA target in btl_util.findAllBtlData(cmd.tar_id))
                if (target.bi.cover != 0)
                    if (FF9StateSystem.Battle.FF9Battle.cur_cmd_list.FindIndex(otherCmd => otherCmd != cmd && otherCmd.info.cover != 0 && (otherCmd.tar_id & target.btl_id) != 0) < 0)
                    {
                        if (!btl_util.IsBtlBusy(target.bi.cover_unit, btl_util.BusyMode.CASTER))
                            btl_mot.setBasePos(target.bi.cover_unit);
                        target.bi.cover_unit = null;
                        btl_mot.setBasePos(target);
                        btl_mot.SetDefaultIdle(target);
                    }

        if (cmd.info.dodge != 0)
            foreach (BTL_DATA target in btl_util.findAllBtlData(cmd.tar_id))
            {
                Boolean stillDodging = false;
                foreach (CMD_DATA runningCmd in btlsys.cur_cmd_list)
                    if (runningCmd != cmd && runningCmd.info.dodge != 0 && (runningCmd.tar_id & target.btl_id) != 0)
                        stillDodging = true;
                if (stillDodging)
                    continue;
                BattleUnit unit = new BattleUnit(target);
                unit.IsDodged = false;

                if (unit.IsPlayer)
                    btl_mot.SetDefaultIdle(unit.Data);
                else if (unit.IsSlave)
                    unit = btl_util.GetMasterEnemyBtlPtr();

                if (Configuration.Battle.FloatEvadeBonus > 0 && !unit.IsPlayer && unit.IsUnderPermanentStatus(BattleStatus.Float))
                    unit.Data.pos[1] = unit.Data.base_pos[1];
                unit.Data.pos[2] = unit.Data.base_pos[2];
            }

        if (caster != null)
        {
            if (cmd.regist != null && cmd == caster.Data.cmd[0])
                UIManager.Battle.RemovePlayerFromAction(cmd.regist.btl_id, true);

            if (caster.IsPlayer && FF9StateSystem.Settings.IsATBFull)
                caster.CurrentAtb = (Int16)(caster.MaximumAtb - 1);

            btl_para.CheckPointData(cmd.regist);
        }

        btlsys.cur_cmd_list.Remove(cmd);
        if (cmd.info.cmd_motion)
            btl_mot.EndCommandMotion(cmd);
        ClearCommand(cmd);

        if (!FF9StateSystem.Battle.isDebug)
            HonoluluBattleMain.playerEnterCommand = true;
    }

    private static Boolean IsNeedToDecreaseTrance(BattleUnit caster, BattleCommandId commandId, CMD_DATA cmd)
    {
        if (!caster.IsUnderStatus(BattleStatus.Trance))
            return false;

        if (commandId == BattleCommandId.Jump || commandId == BattleCommandId.Jump2)
            return false;

        if (commandId == BattleCommandId.Change && cmd.sub_no == 96)
            return false;

        return true;
    }

    private static void ResetCurrentBattlerActiveTime(BattleUnit btl1)
    {
        if (Configuration.Fixes.IsKeepRestTimeInBattle && btl1.MaximumAtb > 0)
            btl1.CurrentAtb = (Int16)Math.Max(0, btl1.CurrentAtb - btl1.MaximumAtb);
        else
            btl1.CurrentAtb = 0;

        btl1.Data.sel_mode = 0;
    }

    public static UInt16 MergeReflecTargetID(REFLEC_DATA reflec)
    {
        UInt16 num = 0;
        for (Int32 index = 0; index < 4; ++index)
            num |= reflec.tar_id[index];
        return num;
    }

    public static void CheckCommandLoop(CMD_DATA cmd)
    {
        Boolean stillRunning = !Configuration.Battle.SFXRework && SFX.isRunning && FF9StateSystem.Battle.FF9Battle.cur_cmd == cmd;
        foreach (UnifiedBattleSequencer.BattleAction action in UnifiedBattleSequencer.runningActions)
            if (action.cmd == cmd)
			{
                stillRunning = true;
                break;
            }
        if (stillRunning)
        {
            if (!Configuration.Battle.SFXRework && cmd.cmd_no == BattleCommandId.SysTrans && SFX.frameIndex == 75)
            {
                if (btl_stat.CheckStatus(cmd.regist, BattleStatus.Trance))
                {
                    btl_vfx.SetTranceModel(cmd.regist, true);
                    BattleAchievement.UpdateTranceStatus();
                }
                else
                {
                    btl_vfx.SetTranceModel(cmd.regist, false);
                }
            }
        }
        else
        {
            BTL_DATA btlData = cmd.regist;
            if (!Configuration.Battle.SFXRework)
            {
                if (!FF9StateSystem.Battle.isDebug && (UIManager.Battle.BtlWorkLibra || UIManager.Battle.BtlWorkPeep))
                    return;
                if (cmd.cmd_no == BattleCommandId.Defend)
                {
                    //if (btl_mot.checkMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_DEF))
                    //{
                    //    if (btlData.evt.animFrame < GeoAnim.geoAnimGetNumFrames(btlData))
                    //        return;
                    //    btl_mot.setMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_DEFENCE);
                    //    btlData.evt.animFrame = 0;
                    //}
                }
                else if (cmd.cmd_no == BattleCommandId.Change)
                {
                    if (Configuration.Battle.NoAutoTrance && cmd.sub_no == 96)
                    {
                        if (btl_mot.checkMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD))
                        {
                            if (!btlData.animEndFrame)
                                return;
                            btl_mot.setMotion(btlData, (Byte)(29U + btlData.bi.row));
                            btlData.evt.animFrame = 0;
                        }
                        if (btl_mot.checkMotion(btlData, (Byte)(29U + btlData.bi.row)))
                        {
                            if (btlData.evt.animFrame < GeoAnim.getAnimationLoopFrame(btlData))
                                return;
                            cmd.info.effect_counter++;
                            ExecVfxCommand(btlData, cmd);
                            //btl_mot.setMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_CMD_TO_NORMAL);
                            //btlData.evt.animFrame = 0;
                            //return;
                        }
                        else
                        {
                            return;
                        }
                        //if (btl_mot.checkMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_CMD_TO_NORMAL))
                        //{
                        //    if (!btlData.animEndFrame)
                        //        return;
                        //    btl_mot.setMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL);
                        //    btlData.evt.animFrame = 0;
                        //}
                    }
                    else
                    {
                        if (btl_mot.checkMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD))
                        {
                            if (!btlData.animEndFrame)
                                return;
                            btl_mot.setMotion(btlData, (Byte)(29U + btlData.bi.row));
                            btlData.evt.animFrame = 0;
                        }
                        if (btl_mot.checkMotion(btlData, (Byte)(29U + btlData.bi.row)))
                        {
                            UInt16 numFrames = (UInt16)GeoAnim.getAnimationLoopFrame(btlData);
                            if (btlData.evt.animFrame < numFrames)
                            {
                                UInt16 num2 = (UInt16)(btlData.evt.animFrame + 1);
                                btlData.pos[2] = btlData.bi.row == 0 ? 400 * num2 / numFrames - 1960 : -400 * num2 / numFrames - 1560;
                                btlData.gameObject.transform.localPosition = btlData.pos;
                                return;
                            }
                            btlData.pos[2] = btlData.bi.row == 0 ? -1560 : -1960;
                            btlData.gameObject.transform.localPosition = btlData.pos;
                            cmd.info.effect_counter++;
                            ExecVfxCommand(btlData, cmd);
                            //btl_mot.setMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_CMD_TO_NORMAL);
                            //btlData.evt.animFrame = 0;
                            //return;
                        }
                        else
						{
                            return;
						}
                        //if (btl_mot.checkMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_CMD_TO_NORMAL))
                        //{
                        //    if (!btlData.animEndFrame)
                        //        return;
                        //    btl_mot.setMotion(btlData, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL);
                        //    btlData.evt.animFrame = 0;
                        //}
                    }
                }
                if (cmd.regist != null)
                    cmd.regist.animFlag &= (UInt16)~EventEngine.afLoop;
                if (cmd.vfxRequest.mexe != null)
                    cmd.vfxRequest.mexe.animFlag &= (UInt16)~EventEngine.afLoop;
            }
            if (cmd.regist != null && cmd.regist.bi.player != 0)
                BattleAchievement.UpdateCommandAchievement(cmd);
            ReqFinishCommand(cmd);
            if (cmd.cmd_no == BattleCommandId.SysTrans)
                btl_stat.SetPresentColor(btlData);
        }
    }

    public static void ExecVfxCommand(BTL_DATA target, CMD_DATA cmd = null)
    {
        if (cmd == null)
            cmd = btl_util.getCurCmdPtr();
        if (cmd == null)
        {
            Debug.LogError("no command!");
            return;
        }

        BTL_DATA caster = cmd.regist;
        BattleCommandId num = cmd.cmd_no;
        switch (num)
        {
            case BattleCommandId.Jump:
            case BattleCommandId.Jump2:
                caster.tar_mode = 2;
                caster.SetDisappear(true, 2);
                break;
            case BattleCommandId.Item:
            case BattleCommandId.AutoPotion:
                UIManager.Battle.ItemUse(cmd.sub_no);
                SBattleCalculator.CalcMain(caster, target, new BattleCommand(cmd), ff9item._FF9Item_Info[btl_util.btlItemNum(cmd.sub_no)].Ref.ScriptId);
                break;
            case BattleCommandId.Throw:
                UIManager.Battle.ItemUse(cmd.sub_no);
                SBattleCalculator.CalcMain(caster, target, new BattleCommand(cmd), cmd.ScriptId);
                break;
            case BattleCommandId.SysLastPhoenix:
                UInt16 battleId = btl_scrp.GetBattleID(1U);
                UInt16 statusBtlId = btl_util.GetStatusBtlID(1U, BattleStatus.BattleEnd);
                if (battleId == 0 || battleId == statusBtlId)
                {
                    FF9StateBattleSystem btlsys = FF9StateSystem.Battle.FF9Battle;
                    UIManager.Battle.FF9BMenu_EnableMenu(false);
                    if (btlsys.btl_phase != 5)
                    {
                        btlsys.btl_phase = 5;
                        btlsys.btl_seq = 0;
                        KillAllCommand(btlsys);
                    }
                }
                SBattleCalculator.CalcMain(caster, target, new BattleCommand(cmd), cmd.ScriptId);
                return;
            case BattleCommandId.SysTrans:
                if (btl_stat.CheckStatus(cmd.regist, BattleStatus.Trance))
                {
                    btl_vfx.SetTranceModel(cmd.regist, true);
                    BattleAchievement.UpdateTranceStatus();
                }
                else
                {
                    btl_vfx.SetTranceModel(cmd.regist, false);
                }
                return;
            default:
                if (cmd.cmd_no == BattleCommandId.Change && cmd.sub_no == 96)
                    cmd.ScriptId = 96;

                if (cmd.sub_no == 176)
                    SBattleCalculator.CalcMain(caster, target, new BattleCommand(cmd), caster.weapon.Ref.ScriptId);
                else
                    SBattleCalculator.CalcMain(caster, target, new BattleCommand(cmd), cmd.ScriptId);
                return;
        }
    }

    public static void DispSelectCursor(FF9StateGlobal sys, FF9StateBattleSystem btlsys, BTL_DATA btl)
    {
        GameObject gameObject = btlsys.s_cur;
        Vector3 localPosition = btl.gameObject.transform.localPosition;
        Vector3 eulerAngles = gameObject.transform.localRotation.eulerAngles;
        gameObject.transform.localPosition = new Vector3(localPosition.x, localPosition.y + btl.height, localPosition.z);
        Single num = (Single)((((btlsys.btl_cnt & 15) << 8) + 1265) % 4096 / 4096.0 * 360.0);
        gameObject.transform.localRotation = Quaternion.Euler(eulerAngles.x, -num, eulerAngles.z);
    }

    private static void DecideSummonType(CMD_DATA cmd)
    {
        AchievementState achievement = FF9StateSystem.Achievement;
        if (cmd.sub_no == 49 && !achievement.summon_shiva ||
            cmd.sub_no == 51 && !achievement.summon_ifrit ||
            cmd.sub_no == 53 && !achievement.summon_ramuh ||
            cmd.sub_no == 55 && !achievement.summon_atomos ||
            cmd.sub_no == 58 && !achievement.summon_odin ||
            cmd.sub_no == 60 && !achievement.summon_leviathan ||
            cmd.sub_no == 62 && !achievement.summon_bahamut ||
            cmd.sub_no == 64 && !achievement.summon_arc ||
            cmd.sub_no == 68 && !achievement.summon_carbuncle_haste ||
            cmd.sub_no == 69 && !achievement.summon_carbuncle_protect ||
            cmd.sub_no == 70 && !achievement.summon_carbuncle_reflector ||
            cmd.sub_no == 71 && !achievement.summon_carbuncle_shell ||
            cmd.sub_no == 66 && !achievement.summon_fenrir_earth ||
            cmd.sub_no == 67 && achievement.summon_fenrir_wind ||
            cmd.sub_no == 72 && !achievement.summon_phoenix ||
            cmd.sub_no == 74 && !achievement.summon_madeen)
            return;

        if (cmd.regist.cur.mp > cmd.aa.MP * 2)
        {
            if (Comn.random8() < 230)
                cmd.info.short_summon = 1;
        }
        else
        {
            if (Comn.random8() < 170)
                cmd.info.short_summon = 1;
        }
    }

    private static Boolean DecideMagicSword(BattleUnit steiner, Int32 mp)
    {
        if (steiner.CurrentMp < mp || steiner.IsUnderStatus((BattleStatus)318905611U))
        {
            UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CombinationFailed);
            return false;
        }

        foreach (BattleUnit unit in FF9StateSystem.Battle.FF9Battle.EnumerateBattleUnits())
        {
            if (btl_util.getSerialNumber(unit.Data) != 2)
                continue;

            if (!unit.IsUnderStatus((BattleStatus)318905611U))
                return true;

            break;
        }

        UIManager.Battle.SetBattleFollowMessage((Int32)BattleMesages.CombinationFailed);
        return false;
    }

    private static void DecideMeteor(CMD_DATA cmd)
    {
        if (cmd.regist.level / 2 + cmd.regist.elem.wpr >= Comn.random16() % 100)
            return;
        cmd.info.meteor_miss = 1;
    }

    private static Boolean ConsumeMp(BTL_DATA btl, Int32 mp)
    {
        if (btl == null)
            return false;

        BattleUnit unit = new BattleUnit(btl);

        if (unit.CurrentMp < mp)
            return false;

        if (!FF9StateSystem.Battle.isDebug && (btl.bi.player == 0 || !FF9StateSystem.Settings.IsHpMpFull))
            btl.cur.mp = (UInt32)(btl.cur.mp - mp);

        return true;
    }

    public static Boolean HasSupportAbility(BTL_DATA btl, SupportAbility1 ability)
    {
        return (btl.sa[0] & (UInt32)ability) != 0;
    }

    public static Boolean HasSupportAbility(BTL_DATA btl, SupportAbility2 ability)
    {
        return (btl.sa[1] & (UInt32)ability) != 0;
    }

    // Might want to add a Boolean flag in Memoria.Data.CharacterCommand instead?
    public static List<BattleCommandId> half_trance_cmd_list = new List<BattleCommandId>
    {
        BattleCommandId.DoubleWhiteMagic,
        BattleCommandId.DoubleBlackMagic
    };

    public const Int32 cmd_delay_max = 10;
    public static Int32 next_cmd_delay;
}
