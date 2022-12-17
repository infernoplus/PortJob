using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonFunc;
using SoulsFormats;

namespace PortJob {
    /* Manages params. Simple as that. */
    public class Paramanger {
        public enum ParamType {
            ActionButtonParam, AiSoundParam, AtkParam_Npc, AtkParam_Pc, AttackElementCorrectParam, BehaviorParam, BehaviorParam_PC,
            BonfireWarpParam, BudgetParam, Bullet, BulletCreateLimitParam, CalcCorrectGraph, Ceremony, CharacterLoadParam, CharaInitParam,
            CharMakeMenuListItemParam, CharMakeMenuTopParam, ClearCountCorrectParam, CoolTimeParam, CultSettingParam, DecalParam,
            DirectionCameraParam, EquipMtrlSetParam, EquipParamAccessory, EquipParamGoods, EquipParamProtector, EquipParamWeapon,
            FaceGenParam, FaceParam, FaceRangeParam, FootSfxParam, GameAreaParam, GameProgressParam, GemCategoryParam, GemDropDopingParam,
            GemDropModifyParam, GemeffectParam, GemGenParam, HitEffectSeParam, HitEffectSfxConceptParam, HitEffectSfxParam, HitMtrlParam,
            HPEstusFlaskRecoveryParam, ItemLotParam, KnockBackParam, KnowledgeLoadScreenItemParam, LoadBalancerDrawDistScaleParam,
            LoadBalancerParam, LockCamParam, LodParam, LodParam_ps4, LodParam_xb1, Magic, MapMimicryEstablishmentParam, MenuOffscrRendParam,
            MenuPropertyLayoutParam, MenuPropertySpecParam, MenuValueTableParam, ModelSfxParam, MoveParam, MPEstusFlaskRecoveryParam,
            MultiHPEstusFlaskBonusParam, MultiMPEstusFlaskBonusParam, MultiPlayCorrectionParam, MultiSoulBonusRateParam, NetworkAreaParam,
            NetworkMsgParam, NetworkParam, NewMenuColorTableParam, NpcAiActionParam, NpcParam, NpcThinkParam, ObjActParam,
            ObjectMaterialSfxParam, ObjectParam, PhantomParam, PlayRegionParam, ProtectorGenParam, RagdollParam, ReinforceParamProtector,
            ReinforceParamWeapon, RoleParam, SeMaterialConvertParam, ShopLineupParam, SkeletonParam, SpEffectParam, SpEffectVfxParam,
            SwordArtsParam, TalkParam, ThrowDirectionSfxParam, ThrowParam, ToughnessParam, UpperArmParam, WeaponGenParam, WepAbsorpPosParam,
            WetAspectParam, WhiteSignCoolTimeParam, Wind
        }

        public Dictionary<ParamType, PARAM> paramz;
        public Paramanger() {
            paramz = new();
            BND4 paramBnd = BND4.Read(Utility.GetEmbededResourceBytes("CommonFunc.Resources.param.parambnd.dcx"));
            BND4 paramdefBnd = BND4.Read(Utility.GetEmbededResourceBytes("CommonFunc.Resources.paramdef.paramdefbnd.dcx"));
            foreach (BinderFile file1 in paramBnd.Files) {
                PARAM param = PARAM.Read(file1.Bytes);
                ParamType type = (ParamType)Enum.Parse(typeof(ParamType), Path.GetFileNameWithoutExtension(file1.Name));

                foreach(BinderFile file2 in paramdefBnd.Files) {
                    string paramName = param.ParamType;
                    string paramDefName = Path.GetFileNameWithoutExtension(file2.Name);
                    if(paramName == paramDefName) {
                        PARAMDEF paramdef = PARAMDEF.Read(file2.Bytes);
                        param.ApplyParamdef(paramdef);
                        break;
                    }
                }
                
                paramz.Add(type, param);
            }
        }

        public PARAM.Row GetRow(ParamType type, int id) {
            PARAM param = paramz[ParamType.ModelSfxParam];
            foreach(PARAM.Row row in param.Rows) {
                if(row.ID == id) { return row; }
            }
            return null;
        }

        public void SetCell(PARAM.Row row, string cellName, Object value) {
            foreach (PARAM.Cell cell in row.Cells) {
                if (
                    cell.Def.DisplayName.ToString().ToLower() == cellName.ToLower() ||
                    cell.Def.InternalName.ToString().ToLower() == cellName.ToLower()
                ) {
                    cell.Value = value; return;
                }
            }
            Console.WriteLine($" ## FAILED TO WRITE TO CELL: {cellName} -> {value}");
        }

        /* Creates the param needed for an object that has a dynamic light sfx param */
        public void CreateLightModelSFXParam(ObjectInfo objectInfo, FXRInfo fxrInfo) {
            PARAM param = paramz[ParamType.ModelSfxParam];

            /* Find the dummy marker we should use */
            int dummyId = -1;
            foreach (KeyValuePair<string, short> dmy in objectInfo.model.dummies) {
                foreach (string id in fxrInfo.template.nodeIdentifiers) {
                    if (dmy.Key.Contains(id)) {
                        dummyId = dmy.Value;
                    }
                }
                if(dummyId != -1) { break; }
            }
            if (dummyId == -1) { dummyId = MassConvert.OBJ_DUMMY_LIST[MassConvert.Dummy.Center]; } // Fall back to center

            PARAM.Row row = new PARAM.Row(objectInfo.id*1000, objectInfo.name, param.AppliedParamdef);
            SetCell(row, "VfxId1", fxrInfo.id);
            SetCell(row, "DummyPolyId1", dummyId);

            param.Rows.Add(row);
        }

        public void Write(string dir) {
            BND4 paramBnd = new BND4();
            foreach (KeyValuePair<ParamType, PARAM> param in paramz) {
                string path = @"Param\RemoveParamDesc\dlc2\";
                paramBnd.Files.Add(new BinderFile(Binder.FileFlags.Flag1, 0, $"{path}{param.Key}.param", param.Value.Write()));
            }
            paramBnd.Write($"{dir}param\\gameparam\\gameparam_dlc2.parambnd.dcx", DCX.Type.DCX_DFLT_10000_44_9);
        }
    }
}
