using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using System.Linq;

namespace MMD
{
	namespace PMD
	{
		public class PMDConverter
		{
			/// <summary>
            /// 着色器类型
			/// </summary>
			public enum ShaderType
			{
				Default,		/// Unity的默认shader
				HalfLambert,	/// 阴影？
                MMDShader		/// MMD 的shader
			}
			
			/// <summary>
			/// 生成GameObject
			/// </summary>
			/// <param name='format'>内部格式的数据</param>
            /// <param name='shader_type'>shader的种类</param>
			/// <param name='use_rigidbody'>是否使用刚体</param>
			/// <param name='use_mecanim'>是否使用Mecanim</param>
			/// <param name='use_ik'>是否使用IK</param>
			/// <param name='scale'>缩放</param>
			public static GameObject CreateGameObject(PMDFormat format, ShaderType shader_type, bool use_rigidbody, bool use_mecanim, bool use_ik, float scale) {
				PMDConverter converter = new PMDConverter();
				return converter.CreateGameObject_(format, shader_type, use_rigidbody, use_mecanim, use_ik, scale);
			}

			/// <summary>
            /// 默认构造函数
			/// </summary>
			/// <remarks>
			/// 禁止用户实例化
			/// </remarks>
			private PMDConverter() {}

			private GameObject CreateGameObject_(PMDFormat format, ShaderType shader_type, bool use_rigidbody, bool use_mecanim, bool use_ik, float scale) {
				format_ = format;
				shader_type_ = shader_type;
				use_rigidbody_ = use_rigidbody;
				use_mecanim_ = use_mecanim;
				use_ik_ = use_ik;
				scale_ = scale;
				root_game_object_ = new GameObject(format_.name);
			
				Mesh mesh = CreateMesh();					// 网格的生成和设定
                Material[] materials = CreateMaterials();	// 材质的生成和设定
                GameObject[] bones = CreateBones();			// 骨骼的生成和设定

                // 创建一个绑定姿势
				BuildingBindpose(mesh, materials, bones);
				root_game_object_.AddComponent<Animation>();	// 增加动画
		
				MMDEngine engine = root_game_object_.AddComponent<MMDEngine>();
		
				// IK注册
				if (use_ik_)
					engine.ik_list = EntryIKSolver(bones);
		
				// 关联刚体
				if (use_rigidbody_)
				{
					try
					{
						var rigids = CreateRigids(bones);
						AssignRigidbodyToBone(bones, rigids);
						SetRigidsSettings(bones, rigids);
						GameObject[] joints = SettingJointComponent(bones, rigids);
						GlobalizeRigidbody(joints);

                        // 非碰撞组
						List<int>[] ignoreGroups = SettingIgnoreRigidGroups(rigids);
						int[] groupTarget = GetRigidbodyGroupTargets(rigids);
		
						MMDEngine.Initialize(engine, scale_, groupTarget, ignoreGroups, rigids);
					}
					catch { }
				}
		
				// Mecanim设定 (not work yet..)
#if UNITY_4_0 || UNITY_4_1
				if (use_mecanim_) {
					AvatarSettingScript avt_setting = new AvatarSettingScript(root_game_object_);
					avt_setting.SettingAvatar();
				}
#endif

				return root_game_object_;
			}

			Vector3[] EntryVertices()
			{
				int vcount = (int)format_.vertex_list.vert_count;
				Vector3[] vpos = new Vector3[vcount];
				for (int i = 0; i < vcount; i++)
					vpos[i] = format_.vertex_list.vertex[i].pos * scale_;
				return vpos;
			}
			
			Vector3[] EntryNormals()
			{
				int vcount = (int)format_.vertex_list.vert_count;
				Vector3[] normals = new Vector3[vcount];
				for (int i = 0; i < vcount; i++)
					normals[i] = format_.vertex_list.vertex[i].normal_vec;
				return normals;
			}
			
			Vector2[] EntryUVs()
			{
				int vcount = (int)format_.vertex_list.vert_count;
				Vector2[] uvs = new Vector2[vcount];
				for (int i = 0; i < vcount; i++)
					uvs[i] = format_.vertex_list.vertex[i].uv;
				return uvs;
			}
			
			BoneWeight[] EntryBoneWeights()
			{
				int vcount = (int)format_.vertex_list.vert_count;
				BoneWeight[] weights = new BoneWeight[vcount];
				for (int i = 0; i < vcount; i++)
				{
					weights[i].boneIndex0 = (int)format_.vertex_list.vertex[i].bone_num[0];
					weights[i].boneIndex1 = (int)format_.vertex_list.vertex[i].bone_num[1];
					weights[i].weight0 = (float)format_.vertex_list.vertex[i].bone_weight / 100.0f;
					weights[i].weight1 = 1.0f - weights[i].weight0;
				}
				return weights;
			}
			
			// 定点坐标和UV之类的注册
			void EntryAttributesForMesh(Mesh mesh)
			{
				//mesh.vertexCount = (int)format_.vertex_list.vert_count;
				mesh.vertices = EntryVertices();
				mesh.normals = EntryNormals();
				mesh.uv = EntryUVs();
				mesh.boneWeights = EntryBoneWeights();
			}
			
			void SetSubMesh(Mesh mesh)
			{
                // 材料与子网格
				// 使子网格与材质与表面顶点相适应
				// 在这里设置表面材质
				mesh.subMeshCount = (int)format_.material_list.material_count;
				
				int sum = 0;
				for (int i = 0; i < mesh.subMeshCount; i++)
				{
					int count = (int)format_.material_list.material[i].face_vert_count;
					int[] indices = new int[count];
					
					// 面顶点从0开始递增
					for (int j = 0; j < count; j++)
						indices[j] = format_.face_vertex_list.face_vert_index[j+sum];
					mesh.SetTriangles(indices, i);
					sum += (int)format_.material_list.material[i].face_vert_count;
				}
			}
			
			// 在Project中注册mesh
			void CreateAssetForMesh(Mesh mesh)
			{
				AssetDatabase.CreateAsset(mesh, format_.folder + "/" + format_.name + ".asset");
			}
			
			Mesh CreateMesh()
			{
				Mesh mesh = new Mesh();
				EntryAttributesForMesh(mesh);
				SetSubMesh(mesh);
				CreateAssetForMesh(mesh);
				return mesh;
			}
			
			//材质是否透明（true:透明, false:不透明)
			bool IsTransparentMaterial(PMD.PMDFormat.Material model_material, Texture2D texture) {
				bool result = false;
				result = result || (model_material.alpha < 0.98f); //0.98f看作不透明(在0.98f阴影生成信息被隐蔽了的原因)
				if (null != texture) {
#if UNITY_4_2
					result = result || texture.alphaIsTransparency;
#else
					// TODO: 在上面的IF中你必须有一个替代的代码
					//result = result;
#endif
				}
				return result;
			}
			
			//是否有边缘
			bool IsEdgeMaterial(PMD.PMDFormat.Material model_material) {
				bool result;
				if (0 == model_material.edge_flag) {
					//无边缘
					result = false;
				} else {
					//有边缘
					result = true;
				}
				return result;
			}
			
			//背面材料 
			bool IsCullBackMaterial(PMD.PMDFormat.Material model_material) {
				bool result;
				if (1.0f <= model_material.alpha) {
					//不透明的话
					//背面
					result = true;
				} else if (0.99f <= model_material.alpha) {
					//不透明的两面绘制
					//背面不
					result = false;
				} else {
					//透明的话
					//背面不
					result = false;
				}
				return result;
			}
			
			//无影 材质
			bool IsNoCastShadowMaterial(PMD.PMDFormat.Material model_material) {
				bool result;
				if (0 == model_material.edge_flag) {
                    //无边缘
					//无影
					result = true;
				} else {
					//有阴影的话
					//投影
					result = false;
				}
				return result;
			}
			
			//是否接受阴影(true:不接受, false:接受)
			bool IsNoReceiveShadowMaterial(PMD.PMDFormat.Material model_material) {
				bool result;
				if (0.98f == model_material.alpha) { //这是一个浮点比较，但是这一次是比较0.98f 和0.98 PMX编辑器
					//不接受(不透明度是0.98f特别处理不接受阴影)的话
					result = true;
				} else {
					//接受阴影的话
					result = false;
				}
				return result;
			}
			
			string GetMMDShaderPath(PMD.PMDFormat.Material model_material, Texture2D texture) {
				string result = "MMD/";
				if (IsTransparentMaterial(model_material, texture)) {
					result += "Transparent/";
				}
				result += "PMDMaterial";
				if (IsEdgeMaterial(model_material)) {
					result += "-with-Outline";
				}
				if (IsCullBackMaterial(model_material)) {
					result += "-CullBack";
				}
				if (IsNoCastShadowMaterial(model_material)) {
					result += "-NoCastShadow";
				}
#if MFU_ENABLE_NO_RECEIVE_SHADOW_SHADER	//没有阴影shader无效化
				if (IsNoReceiveShadowMaterial(model_material)) {
					result += "-NoReceiveShadow";
				}
#endif //MFU_ENABLE_NO_RECEIVE_SHADOW_SHADER
				return result;
			}

			// 生成颜色
			void EntryColors(Material[] mats)
			{
				// 生成材质 
				for (int i = 0; i < mats.Length; i++)
				{
					// PMD格式化取得材质 
					PMD.PMDFormat.Material pmdMat = format_.material_list.material[i];
					
					//受限检查贴图信息
					Texture2D main_texture = null;
					if (pmdMat.texture_file_name != "") {
						string path = format_.folder + "/" + pmdMat.texture_file_name;
						main_texture = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
					}
					
					//进行材质的设定
					switch (shader_type_)
					{
						case ShaderType.Default:	// 默认
							mats[i] = new Material(Shader.Find("Transparent/Diffuse"));
							mats[i].color = pmdMat.diffuse_color;
							Color cbuf = mats[i].color;
							cbuf.a = pmdMat.alpha;	// 这样可以么
							mats[i].color = cbuf;
							break;

                        case ShaderType.HalfLambert:	// HalfLambert
							mats[i] = new Material(Shader.Find("Custom/CharModel"));
							mats[i].SetFloat("_Cutoff", 1 - pmdMat.alpha);
							mats[i].color = pmdMat.diffuse_color;
							break;

						case ShaderType.MMDShader:
							mats[i] = new Material(Shader.Find(GetMMDShaderPath(pmdMat, main_texture)));

                            // 所有设置或没有或有值取决于着色，设置尽量不要设置错误
							mats[i].SetColor("_Color", pmdMat.diffuse_color);
							mats[i].SetColor("_AmbColor", pmdMat.mirror_color);
							mats[i].SetFloat("_Opacity", pmdMat.alpha);
							mats[i].SetColor("_SpecularColor", pmdMat.specular_color);
							mats[i].SetFloat("_Shininess", pmdMat.specularity);

                            //　边缘
							mats[i].SetFloat("_OutlineWidth", 0.2f);	// 这样感觉很好

                            // sphere_map
							string path = format_.folder + "/" + pmdMat.sphere_map_name;
							Texture sphere_map;

							if (File.Exists(path))
							{	//　确认文件是否存在
								sphere_map = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Texture)) as Texture;
								
								
								string ext = Path.GetExtension(pmdMat.sphere_map_name);
								switch (ext) {
								case ".spa": // 加
									mats[i].SetTexture("_SphereAddTex", sphere_map);
									mats[i].SetTextureScale("_SphereAddTex", new Vector2(1, -1));
									break;
								case ".sph": // 乘
									mats[i].SetTexture("_SphereMulTex", sphere_map);
									mats[i].SetTextureScale("_SphereMulTex", new Vector2(1, -1));
									break;
								default:
									// 加
									goto case ".spa";
								}
							}

                            // 取得toon的位置
							string toon_name = pmdMat.toon_index != 0xFF ?
								format_.toon_texture_list.toon_texture_file[pmdMat.toon_index] : "toon00.bmp";
							string resource_path = UnityEditor.AssetDatabase.GetAssetPath(Shader.Find("MMD/HalfLambertOutline"));
							resource_path = Path.GetDirectoryName(resource_path);	// 取得resource文件夹
							resource_path += "/toon/" + toon_name;

                            // 是否存在toon
							if (!File.Exists(resource_path))
							{
                                // 有存在自身的toon的可能性
								resource_path = format_.folder + "/" + format_.toon_texture_list.toon_texture_file[pmdMat.toon_index];
								if (!File.Exists(resource_path))
								{
									Debug.LogError("Do not exists toon texture: " + format_.toon_texture_list.toon_texture_file[pmdMat.toon_index]);
									break;
								}
							}

							// 贴图的分配
							Texture toon_tex = UnityEditor.AssetDatabase.LoadAssetAtPath(resource_path, typeof(Texture)) as Texture;
							mats[i].SetTexture("_ToonTex", toon_tex);
							mats[i].SetTextureScale("_ToonTex", new Vector2(1, -1));
							break;
					}

                    // 纹理不是空的则注册
					if (null != main_texture) {
						mats[i].mainTexture = main_texture;
						mats[i].mainTextureScale = new Vector2(1, -1);
					}
				}
			}
			
			// 对材质等必要的颜色进行注册
			Material[] EntryAttributesForMaterials()
			{
				int count = (int)format_.material_list.material_count;
				Material[] mats = new Material[count];
				EntryColors(mats);
				return mats;
			}
			
			// 对材质进行注册
			void CreateAssetForMaterials(Material[] mats)
			{
				// 放入适当的文件夹
				string path = format_.folder + "/Materials/";
				if (!System.IO.Directory.Exists(path)) { 
					AssetDatabase.CreateFolder(format_.folder, "Materials");
				}
				
				for (int i = 0; i < mats.Length; i++)
				{
					string fname = path + format_.name + "_material" + i + ".asset";
					AssetDatabase.CreateAsset(mats[i], fname);
				}
			}
			
			// 生成材质
			Material[] CreateMaterials()
			{
				Material[] materials;
				materials = EntryAttributesForMaterials();
				CreateAssetForMaterials(materials);
				return materials;
			}

			// 构建夫子关系
			void AttachParentsForBone(GameObject[] bones)
			{
				for (int i = 0; i < bones.Length; i++)
				{
					int index = format_.bone_list.bone[i].parent_bone_index;
					if (index != 0xFFFF)
						bones[i].transform.parent = bones[index].transform;
					else
						bones[i].transform.parent = root_game_object_.transform;
				}
			}

			// 决定骨骼的位置和父子关系的准备
			GameObject[] EntryAttributeForBones()
			{
				int count = format_.bone_list.bone_count;
				GameObject[] bones = new GameObject[count];
				
				for (int i = 0; i < count; i++) {
					bones[i] = new GameObject(format_.bone_list.bone[i].bone_name);
					bones[i].transform.name = bones[i].name;
					bones[i].transform.position = format_.bone_list.bone[i].bone_head_pos * scale_;
				}
				return bones;
			}
			
			//生成骨骼
			GameObject[] CreateBones()
			{
				GameObject[] bones;
				bones = EntryAttributeForBones();
				AttachParentsForBone(bones);
				CreateSkinBone(bones);
				return bones;
			}

			// 进行表情骨骼的生成
			void CreateSkinBone(GameObject[] bones)
			{
				// 表情root 添加子表情root
				GameObject skin_root = new GameObject("Expression");
				if (skin_root.GetComponent<ExpressionManagerScript>() == null)
					skin_root.AddComponent<ExpressionManagerScript>();
				skin_root.transform.parent = root_game_object_.transform;
				
				for (int i = 0; i < format_.skin_list.skin_count; i++)
				{
					// 把表情加在父骨骼上
					GameObject skin = new GameObject(format_.skin_list.skin_data[i].skin_name);
					skin.transform.parent = skin_root.transform;
					var script = skin.AddComponent<MMDSkinsScript>();

                    // 元素的信息
					AssignMorphVectorsForSkin(format_.skin_list.skin_data[i], format_.vertex_list, script);
				}
			}

            //  元素的信息（元素Index、移动顶点）进行记录
			void AssignMorphVectorsForSkin(PMD.PMDFormat.SkinData data, PMD.PMDFormat.VertexList vtxs, MMDSkinsScript script)
			{
				uint count = data.skin_vert_count;
				int[] indices = new int[count];
				Vector3[] morph_target = new Vector3[count];

				for (int i = 0; i < count; i++)
				{
					//在这里设定
					indices[i] = (int)data.skin_vert_data[i].skin_vert_index;

					//  元顶点
					//morph_target[i] = (data.skin_vert_data[i].skin_vert_pos - vtxs.vertex[indices[i]].pos).normalized;
					//morph_target[i] = data.skin_vert_data[i].skin_vert_pos - vtxs.vertex[indices[i]].pos;
					morph_target[i] = data.skin_vert_data[i].skin_vert_pos * scale_;
				}

                // 被存储在脚本中
				script.morphTarget = morph_target;
				script.targetIndices = indices;

				switch (data.skin_type)
				{
					case 0:
						script.skinType = MMDSkinsScript.SkinType.Base;
						script.gameObject.name = "base";
						break;

					case 1:
						script.skinType = MMDSkinsScript.SkinType.EyeBrow;
						break;

					case 2:
						script.skinType = MMDSkinsScript.SkinType.Eye;
						break;

					case 3:
						script.skinType = MMDSkinsScript.SkinType.Lip;
						break;

					case 4:
						script.skinType = MMDSkinsScript.SkinType.Other;
						break;
				}
			}

            // 创建一个绑定姿势
			void BuildingBindpose(Mesh mesh, Material[] materials, GameObject[] bones)
			{
				// 矩阵 和变换
				Matrix4x4[] bindpose = new Matrix4x4[bones.Length];
				Transform[] trans = new Transform[bones.Length];
				for (int i = 0; i < bones.Length; i++) {
					trans[i] = bones[i].transform;
					bindpose[i] = bones[i].transform.worldToLocalMatrix;
				}

                // 里应用规模
				SkinnedMeshRenderer smr = root_game_object_.AddComponent<SkinnedMeshRenderer>() as SkinnedMeshRenderer;
				mesh.bindposes = bindpose;
				smr.sharedMesh = mesh;
				smr.bones = trans;
				smr.materials = materials;
				smr.receiveShadows = false; //不接受阴影
				ExpressionManagerScript ems = root_game_object_.GetComponentInChildren<ExpressionManagerScript>();
				ems.mesh = mesh;
			}
			
			// 注册IK
			//   IK对script进行基本使用
			CCDIKSolver[] EntryIKSolver(GameObject[] bones)
			{
				PMD.PMDFormat.IKList ik_list = format_.ik_list;

				CCDIKSolver[] iksolvers = new CCDIKSolver[ik_list.ik_data_count];
				for (int i = 0; i < ik_list.ik_data_count; i++)
				{
					PMD.PMDFormat.IK ik = ik_list.ik_data[i];

					bones[ik.ik_bone_index].AddComponent<CCDIKSolver>();
					CCDIKSolver solver = bones[ik.ik_bone_index].GetComponent<CCDIKSolver>();
					solver.target = bones[ik.ik_target_bone_index].transform;
					solver.controll_weight = ik.control_weight * 4; // PMD文件的大概4倍
					solver.iterations = ik.iterations;
					solver.chains = new Transform[ik.ik_chain_length];
					for (int j = 0; j < ik.ik_chain_length; j++)
						solver.chains[j] = bones[ik.ik_child_bone_index[j]].transform;

					if (!(bones[ik.ik_bone_index].name.Contains("足") || bones[ik.ik_bone_index].name.Contains("つま先")))
					{
						solver.enabled = false;
					}
					iksolvers[i] = solver;
				}

				return iksolvers;
			}

			// Sphere Collider的设定
			Collider EntrySphereCollider(PMDFormat.Rigidbody rigid, GameObject obj)
			{
				SphereCollider collider = obj.AddComponent<SphereCollider>();
				collider.radius = rigid.shape_w * scale_;
				return collider;
			}

            // Box Collider的设定
			Collider EntryBoxCollider(PMDFormat.Rigidbody rigid, GameObject obj)
			{
				BoxCollider collider = obj.AddComponent<BoxCollider>();
				collider.size = new Vector3(
					rigid.shape_w * 2f * scale_,
					rigid.shape_h * 2f * scale_, 
					rigid.shape_d * 2f * scale_);
				return collider;
			}

            // Capsule Collider的设定
			Collider EntryCapsuleCollider(PMDFormat.Rigidbody rigid, GameObject obj)
			{
				CapsuleCollider collider = obj.AddComponent<CapsuleCollider>();
				collider.radius = rigid.shape_w * scale_;
				collider.height = (rigid.shape_h + rigid.shape_w * 2) * scale_;
				return collider;
			}

            // 物理素材的定义
			PhysicMaterial CreatePhysicMaterial(PMDFormat.Rigidbody rigid)
			{
				PhysicMaterial material = new PhysicMaterial(format_.name + "_r" + rigid.rigidbody_name);
				material.bounciness = rigid.rigidbody_recoil;
				material.staticFriction = rigid.rigidbody_friction;
				material.dynamicFriction = rigid.rigidbody_friction;

				AssetDatabase.CreateAsset(material, format_.folder + "/Physics/" + GetFilePathString(material.name) + ".asset");
				return material;
			}

            // Unity方面Rigidbody的设定
			void UnityRigidbodySetting(PMDFormat.Rigidbody rigidbody, GameObject targetBone, bool setted=false)
			{
				// rigidbody的调整
				if (!setted)
				{
					targetBone.GetComponent<Rigidbody>().isKinematic = (0 == rigidbody.rigidbody_type);
					targetBone.GetComponent<Rigidbody>().mass = Mathf.Max(float.Epsilon, rigidbody.rigidbody_weight);
					targetBone.GetComponent<Rigidbody>().drag = rigidbody.rigidbody_pos_dim;
					targetBone.GetComponent<Rigidbody>().angularDrag = rigidbody.rigidbody_rot_dim;
				}
				else
				{
					// Rigidbody在适用骨骼时复数时 取平均
					targetBone.GetComponent<Rigidbody>().mass += rigidbody.rigidbody_weight;
					targetBone.GetComponent<Rigidbody>().drag += rigidbody.rigidbody_pos_dim;
					targetBone.GetComponent<Rigidbody>().angularDrag += rigidbody.rigidbody_rot_dim;
					targetBone.GetComponent<Rigidbody>().mass *= 0.5f;
					targetBone.GetComponent<Rigidbody>().drag *= 0.5f;
					targetBone.GetComponent<Rigidbody>().angularDrag *= 0.5f;
				}
			}

			// 代入刚体的值
			void SetRigidsSettings(GameObject[] bones, GameObject[] rigid)
			{
				PMDFormat.RigidbodyList list = format_.rigidbody_list;
				for (int i = 0; i < list.rigidbody_count; i++)	// i是刚体编号
				{
					// 刚体的关联骨骼的Index
					int rigidRefIndex = list.rigidbody[i].rigidbody_rel_bone_index;

                    // 局部坐标的测定
					Vector3 localPos = list.rigidbody[i].pos_pos * scale_;// - rigid[i].transform.position;

                    // 测定位置
					if (rigidRefIndex >= ushort.MaxValue)
					{
                        //没有相关骨骼
						if (rigid[i].GetComponent<Rigidbody>() == null)
							rigid[i].AddComponent<Rigidbody>();
						UnityRigidbodySetting(list.rigidbody[i], rigid[i]);
						rigid[i].transform.localPosition = localPos;

                        // 没有相关的骨骼的刚体被连接到所述中心骨
						rigid[i].transform.position = localPos + format_.bone_list.bone[0].bone_head_pos * scale_;
                        // 决定的旋转的值
						Vector3 rot = list.rigidbody[i].pos_rot * Mathf.Rad2Deg;
						rigid[i].transform.rotation = Quaternion.Euler(rot);
					}
					else
						//有关联骨骼
					{	// 现在这里进行刚体的添加和设定
						if (bones[rigidRefIndex].GetComponent<Rigidbody>() == null)
							bones[rigidRefIndex].AddComponent<Rigidbody>();
						UnityRigidbodySetting(list.rigidbody[i], bones[rigidRefIndex]);
						rigid[i].transform.localPosition = localPos;
                        //决定的旋转的值
						Vector3 rot = list.rigidbody[i].pos_rot * Mathf.Rad2Deg;
						rigid[i].transform.rotation = Quaternion.Euler(rot);
					}
					
				}
			}

			// 生成刚体
			GameObject[] CreateRigids(GameObject[] bones)
			{
				PMDFormat.RigidbodyList list = format_.rigidbody_list;
				if (!System.IO.Directory.Exists(System.IO.Path.Combine(format_.folder, "Physics")))
				{
					AssetDatabase.CreateFolder(format_.folder, "Physics");
				}
				
				// 注册刚体
				GameObject[] rigid = new GameObject[list.rigidbody_count];
				for (int i = 0; i < list.rigidbody_count; i++)
				{
					rigid[i] = new GameObject("r" + list.rigidbody[i].rigidbody_name);
                    //rigid[i].AddComponent<Rigidbody>();		// 刚体主题不适用于Rigidbody

					// 各种Collider的设定
					Collider collider = null;
					switch (list.rigidbody[i].shape_type)
					{
						case 0:
							collider = EntrySphereCollider(list.rigidbody[i], rigid[i]);
							break;

						case 1:
							collider = EntryBoxCollider(list.rigidbody[i], rigid[i]);
							break;

						case 2:
							collider = EntryCapsuleCollider(list.rigidbody[i], rigid[i]);
							break;
					}

					// 材质的设定
					collider.material = CreatePhysicMaterial(list.rigidbody[i]);
				}
				return rigid;
			}

			// 依据加入的刚体编号进行索引
			int SearchConnectRigidByJoint(int rigidIndex)
			{
				for (int i = 0; i < format_.rigidbody_joint_list.joint_count; i++)
				{
					int joint_rigidbody_a = (int)format_.rigidbody_joint_list.joint[i].joint_rigidbody_a;
					int joint_rigidbody_b = (int)format_.rigidbody_joint_list.joint[i].joint_rigidbody_b;
					if (joint_rigidbody_b == rigidIndex)
					{
						return joint_rigidbody_a;
					}
					else if (joint_rigidbody_a == rigidIndex)
					{
						return joint_rigidbody_b;
					}
				}
				// 不能发现接续刚体
				return -1;
			}

			//从没有关联的刚体搜寻父骨骼
			// rigidIndex是刚体编号
			int GetTargetRigidBone(int rigidIndex)
			{
				// 寻找接续刚体A
				int targetRigid = SearchConnectRigidByJoint(rigidIndex);

				// 寻找接续刚体A关联的骨骼
				int ind = format_.rigidbody_list.rigidbody[targetRigid].rigidbody_rel_bone_index;
				
				// 如果关闭了MaxValue接续刚体A会与关联骨骼相链接？
				if (ind >= ushort.MaxValue)
					format_.rigidbody_list.rigidbody[rigidIndex].rigidbody_rel_bone_index = ushort.MaxValue + (ushort)ind;
				
				return (int)ind;
			}

			// 刚体骨骼、
			void AssignRigidbodyToBone(GameObject[] bones, GameObject[] rigids)
			{
				// 仅仅循环刚体数
				for (int i = 0; i < rigids.Length; i++)
				{
                    // 存储在父骨骼刚体
					int refIndex = format_.rigidbody_list.rigidbody[i].rigidbody_rel_bone_index;
					if (refIndex != ushort.MaxValue)
					{	// 65535是最大值
						rigids[i].transform.parent = bones[refIndex].transform;
					}
					else
					{
						// joint出来的接续刚体B＝用现在的刚体名寻找出来
						int boneIndex = GetTargetRigidBone(i);

						// 在接续刚体A的骨骼上接续刚体
						rigids[i].transform.parent = bones[boneIndex].transform;
					}
				}
			}

			// 移动和旋转的限制
			void SetMotionAngularLock(PMDFormat.Joint joint, ConfigurableJoint conf)
			{
				SoftJointLimit jlim;

				// Motion的固定
				if (joint.constrain_pos_1.x == 0f && joint.constrain_pos_2.x == 0f)
					conf.xMotion = ConfigurableJointMotion.Locked;
				else
					conf.xMotion = ConfigurableJointMotion.Limited;

				if (joint.constrain_pos_1.y == 0f && joint.constrain_pos_2.y == 0f)
					conf.yMotion = ConfigurableJointMotion.Locked;
				else
					conf.yMotion = ConfigurableJointMotion.Limited;

				if (joint.constrain_pos_1.z == 0f && joint.constrain_pos_2.z == 0f)
					conf.zMotion = ConfigurableJointMotion.Locked;
				else
					conf.zMotion = ConfigurableJointMotion.Limited;

				// 角度的固定
				if (joint.constrain_rot_1.x == 0f && joint.constrain_rot_2.x == 0f)
					conf.angularXMotion = ConfigurableJointMotion.Locked;
				else
				{
					conf.angularXMotion = ConfigurableJointMotion.Limited;
					float hlim = Mathf.Max(-joint.constrain_rot_1.x, -joint.constrain_rot_2.x); //回転方向が逆なので負数
					float llim = Mathf.Min(-joint.constrain_rot_1.x, -joint.constrain_rot_2.x);
					SoftJointLimit jhlim = new SoftJointLimit();
					jhlim.limit = Mathf.Clamp(hlim * Mathf.Rad2Deg, -180.0f, 180.0f);
					conf.highAngularXLimit = jhlim;

					SoftJointLimit jllim = new SoftJointLimit();
					jllim.limit = Mathf.Clamp(llim * Mathf.Rad2Deg, -180.0f, 180.0f);
					conf.lowAngularXLimit = jllim;
				}

				if (joint.constrain_rot_1.y == 0f && joint.constrain_rot_2.y == 0f)
					conf.angularYMotion = ConfigurableJointMotion.Locked;
				else
				{
					// 还要注意是负的话会出现误差值
					conf.angularYMotion = ConfigurableJointMotion.Limited;
					float lim = Mathf.Min(Mathf.Abs(joint.constrain_rot_1.y), Mathf.Abs(joint.constrain_rot_2.y));//绝对值小
					jlim = new SoftJointLimit();
					jlim.limit = lim * Mathf.Clamp(Mathf.Rad2Deg, 0.0f, 180.0f);
					conf.angularYLimit = jlim;
				}

				if (joint.constrain_rot_1.z == 0f && joint.constrain_rot_2.z == 0f)
					conf.angularZMotion = ConfigurableJointMotion.Locked;
				else
				{
					conf.angularZMotion = ConfigurableJointMotion.Limited;
					float lim = Mathf.Min(Mathf.Abs(-joint.constrain_rot_1.z), Mathf.Abs(-joint.constrain_rot_2.z));//绝对值小//旋转方向是相反的所以负数
					jlim = new SoftJointLimit();
					jlim.limit = Mathf.Clamp(lim * Mathf.Rad2Deg, 0.0f, 180.0f);
					conf.angularZLimit = jlim;
				}
			}

			// 弹簧的设置之类
			void SetDrive(PMDFormat.Joint joint, ConfigurableJoint conf)
			{
				JointDrive drive;

				// Position
				if (joint.spring_pos.x != 0f)
				{
					drive = new JointDrive();
					drive.positionSpring = joint.spring_pos.x * scale_;
					conf.xDrive = drive;
				}
				if (joint.spring_pos.y != 0f)
				{
					drive = new JointDrive();
					drive.positionSpring = joint.spring_pos.y * scale_;
					conf.yDrive = drive;
				}
				if (joint.spring_pos.z != 0f)
				{
					drive = new JointDrive();
					drive.positionSpring = joint.spring_pos.z * scale_;
					conf.zDrive = drive;
				}

				// Angular
				if (joint.spring_rot.x != 0f)
				{
					drive = new JointDrive();
					drive.mode = JointDriveMode.PositionAndVelocity;
					drive.positionSpring = joint.spring_rot.x;
					conf.angularXDrive = drive;
				}
				if (joint.spring_rot.y != 0f || joint.spring_rot.z != 0f)
				{
					drive = new JointDrive();
					drive.mode = JointDriveMode.PositionAndVelocity;
					drive.positionSpring = (joint.spring_rot.y + joint.spring_rot.z) * 0.5f;
					conf.angularYZDrive = drive;
				}
			}

			// ConfigurableJoint值的设定
			void SetAttributeConfigurableJoint(PMDFormat.Joint joint, ConfigurableJoint conf)
			{
				SetMotionAngularLock(joint, conf);
				SetDrive(joint, conf);
			}

			// ConfigurableJoint的设定
			// 因为要设定在目的上所以设定FixedJoint
			GameObject[] SetupConfigurableJoint(GameObject[] rigids)
			{
				List<GameObject> result_list = new List<GameObject>();
				foreach (PMDFormat.Joint joint in format_.rigidbody_joint_list.joint) {
					//取得相互接续的刚体
					Transform transform_a = rigids[joint.joint_rigidbody_a].transform;
					Transform transform_b = rigids[joint.joint_rigidbody_b].transform;
					Rigidbody rigidbody_a = transform_a.GetComponent<Rigidbody>();
					if (null == rigidbody_a) {
						rigidbody_a = transform_a.parent.GetComponent<Rigidbody>();
					}
					Rigidbody rigidbody_b = transform_b.GetComponent<Rigidbody>();
					if (null == rigidbody_b) {
						rigidbody_b = transform_b.parent.GetComponent<Rigidbody>();
					}
					if (rigidbody_a != rigidbody_b) {
						//如果接续刚体不是同一个刚体的话
						//(原本的PMD只要没有错误的话是不会指向同一个物体的，可能是在MFU上没有取得关联骨骼的刚体连接到中心骨骼上)
						//关节设定
						ConfigurableJoint config_joint = rigidbody_b.gameObject.AddComponent<ConfigurableJoint>();
						config_joint.connectedBody = rigidbody_a;
						SetAttributeConfigurableJoint(joint, config_joint);
						
						result_list.Add(config_joint.gameObject);
					}
				}
				return result_list.ToArray();
			}

			// 关节设定
			// 关节对骨骼来说是适用的
			GameObject[] SettingJointComponent(GameObject[] bones, GameObject[] rigids)
			{
				// ConfigurableJoint的设定
				GameObject[] joints = SetupConfigurableJoint(rigids);
				return joints;
			}

			// 刚体的全局坐标
			void GlobalizeRigidbody(GameObject[] joints)
			{
				if ((null != joints) && (0 < joints.Length)) {
					// 物理计算生成路径在路径上添加子项
					GameObject physics_root = new GameObject("Physics");
					PhysicsManager physics_manager = physics_root.AddComponent<PhysicsManager>();
					physics_root.transform.parent = root_game_object_.transform;
					Transform physics_root_transform = physics_root.transform;
					
					// 对PhysicsManager给一个人运动前的状态(删除重复的，因为重叠了好几次)
					physics_manager.connect_bone_list = joints.Select(x=>x.gameObject)
																.Distinct()
																.Select(x=>new PhysicsManager.ConnectBone(x, x.transform.parent.gameObject))
																.ToArray();
					
					//isKinematic 取得ConfigurableJoint的情况下设置全局坐标
					foreach (ConfigurableJoint joint in joints.Where(x=>!x.GetComponent<Rigidbody>().isKinematic)
																.Select(x=>x.GetComponent<ConfigurableJoint>())) {
						joint.transform.parent = physics_root_transform;
					}
				}
			}

			// 不冲突刚体的设定
			List<int>[] SettingIgnoreRigidGroups(GameObject[] rigids)
			{
				// 非冲突的List初始化
				const int MaxGroup = 16;	// Group的最大数
				List<int>[] ignoreRigid = new List<int>[MaxGroup];
				for (int i = 0; i < 16; i++) ignoreRigid[i] = new List<int>();

				// 将刚体添加到飞冲突刚体组
				PMDFormat.RigidbodyList list = format_.rigidbody_list;
				for (int i = 0; i < list.rigidbody_count; i++)
					ignoreRigid[list.rigidbody[i].rigidbody_group_index].Add(i);
				return ignoreRigid;
			}

			// GroupTarget
			int[] GetRigidbodyGroupTargets(GameObject[] rigids)
			{
				int[] result = new int[rigids.Length];
				for (int i = 0; i < rigids.Length; i++)
				{
					result[i] = format_.rigidbody_list.rigidbody[i].rigidbody_group_target;
				}
				return result;
			}
			
			/// <summary>
			/// 取得文件路径字符串
			/// </summary>
			/// <returns>文件路径的可能字符串</returns>
			/// <param name='src'>文件路径不使用的字符</param>
			private static string GetFilePathString(string src) {
				return src.Replace('\\', '＼')
							.Replace('/',  '／')
							.Replace(':',  '：')
							.Replace('*',  '＊')
							.Replace('?',  '？')
							.Replace('"',  '”')
							.Replace('<',  '＜')
							.Replace('>',  '＞')
							.Replace('|',  '｜')
							.Replace("\n",  string.Empty)
							.Replace("\r",  string.Empty);
			}

			GameObject	root_game_object_;
			PMDFormat	format_;
			ShaderType	shader_type_;
			bool		use_rigidbody_;
			bool		use_mecanim_;
			bool		use_ik_;
			float		scale_;
		}
	}
	
	namespace VMD
	{
		public class VMDConverter
		{
			/// <summary>
			/// 生成AnimationClip
			/// </summary>
			/// <param name='name'>内部形式数据</param>
			/// <param name='assign_pmd'>使用PMD的GameObject</param>
			/// <param name='interpolationQuality'>完成曲线质量</param>
			public static AnimationClip CreateAnimationClip(VMDFormat format, GameObject assign_pmd, int interpolationQuality) {
				VMDConverter converter = new VMDConverter();
				return converter.CreateAnimationClip_(format, assign_pmd, interpolationQuality);
			}

			/// <summary>
			/// 默认的构造方法
			/// </summary>
			/// <remarks>
			/// 禁止用户创建实例
			/// </remarks>
			private VMDConverter() {}

			// 注册动画剪辑
			private AnimationClip CreateAnimationClip_(MMD.VMD.VMDFormat format, GameObject assign_pmd, int interpolationQuality)
			{
				//设定缩放
				scale_ = 1.0f;
				if (assign_pmd) {
					MMDEngine engine = assign_pmd.GetComponent<MMDEngine>();
					if (engine) {
						scale_ = engine.scale;
					}
				}

				//Animation anim = assign_pmd.GetComponent<Animation>();
				
				// 生成clip
				AnimationClip clip = new AnimationClip();
				clip.name = assign_pmd.name + "_" + format.name;
				
				Dictionary<string, string> bone_path = new Dictionary<string, string>();
				Dictionary<string, GameObject> gameobj = new Dictionary<string, GameObject>();
				GetGameObjects(gameobj, assign_pmd);		// 取得子骨骼下的GameObject
				FullSearchBonePath(assign_pmd.transform, bone_path);
				FullEntryBoneAnimation(format, clip, bone_path, gameobj, interpolationQuality);

				CreateKeysForSkin(format, clip);	// 添加表情
				
				return clip;
			}

			// 取得贝塞尔Handle
			// 0～127的值作为 0f～1f返回
			static Vector2 GetBezierHandle(byte[] interpolation, int type, int ab)
			{
				// 0=X, 1=Y, 2=Z, 3=R
				// ab是a?还是b?
				Vector2 bezierHandle = new Vector2((float)interpolation[ab*8+type], (float)interpolation[ab*8+4+type]);
				return bezierHandle/127f;
			}
			// 取得 p0:(0f,0f),p3:(1f,1f)上的贝塞尔曲线
			// t在0～1的范围
			static Vector2 SampleBezier(Vector2 bezierHandleA, Vector2 bezierHandleB, float t)
			{
				Vector2 p0 = Vector2.zero;
				Vector2 p1 = bezierHandleA;
				Vector2 p2 = bezierHandleB;
				Vector2 p3 = new Vector2(1f,1f);
				
				Vector2 q0 = Vector2.Lerp(p0, p1, t);
				Vector2 q1 = Vector2.Lerp(p1, p2, t);
				Vector2 q2 = Vector2.Lerp(p2, p3, t);
				
				Vector2 r0 = Vector2.Lerp(q0, q1, t);
				Vector2 r1 = Vector2.Lerp(q1, q2, t);
				
				Vector2 s0 = Vector2.Lerp(r0, r1, t);
				return s0;
			}
			// 插补曲线或等效的线性内插
			static bool IsLinear(byte[] interpolation, int type)
			{
				byte ax=interpolation[0*8+type];
				byte ay=interpolation[0*8+4+type];
				byte bx=interpolation[1*8+type];
				byte by=interpolation[1*8+4+type];
				return (ax == ay) && (bx == by);
			}
			// 为了取得插补曲线的近似值 获取关键帧所包含的关键帧数
			int GetKeyframeCount(List<MMD.VMD.VMDFormat.Motion> mlist, int type, int interpolationQuality)
			{
				int count = 0;
				for(int i = 0; i < mlist.Count; i++)
				{
					if(i>0 && !IsLinear(mlist[i].interpolation, type))
					{
						count += interpolationQuality;//Interpolation Keyframes
					}
					else
					{
						count += 1;//Keyframe
					}
				}
				return count;
			}
			//关键帧为1时 添加虚拟关键帧
			void AddDummyKeyframe(ref Keyframe[] keyframes)
			{
				if(keyframes.Length==1)
				{
					Keyframe[] newKeyframes=new Keyframe[2];
					newKeyframes[0]=keyframes[0];
					newKeyframes[1]=keyframes[0];
					newKeyframes[1].time+=0.001f/60f;//1[ms]
					newKeyframes[0].outTangent=0f;
					newKeyframes[1].inTangent=0f;
					keyframes=newKeyframes;
				}
			}
			// 具有任意类型value的关键帧
			abstract class CustomKeyframe<Type>
			{
				public CustomKeyframe(float time,Type value)
				{
					this.time=time;
					this.value=value;
				}
				public float time{ get; set; }
				public Type value{ get; set; }
			}
			// float类型value的关键帧
			class FloatKeyframe:CustomKeyframe<float>
			{
				public FloatKeyframe(float time,float value):base(time,value)
				{
				}
				// 线性插值
				public static FloatKeyframe Lerp(FloatKeyframe from, FloatKeyframe to,Vector2 t)
				{
					return new FloatKeyframe(
						Mathf.Lerp(from.time,to.time,t.x),
						Mathf.Lerp(from.value,to.value,t.y)
					);
				}
				// 在线性插值上添加近似的贝塞尔曲线
				public static void AddBezierKeyframes(byte[] interpolation, int type,
					FloatKeyframe prev_keyframe,FloatKeyframe cur_keyframe, int interpolationQuality,
					ref FloatKeyframe[] keyframes,ref int index)
				{
					if(prev_keyframe==null || IsLinear(interpolation,type))
					{
						keyframes[index++]=cur_keyframe;
					}
					else
					{
						Vector2 bezierHandleA=GetBezierHandle(interpolation,type,0);
						Vector2 bezierHandleB=GetBezierHandle(interpolation,type,1);
						int sampleCount = interpolationQuality;
						for(int j = 0; j < sampleCount; j++)
						{
							float t = (j+1)/(float)sampleCount;
							Vector2 sample = SampleBezier(bezierHandleA,bezierHandleB,t);
							keyframes[index++] = FloatKeyframe.Lerp(prev_keyframe,cur_keyframe,sample);
						}
					}
				}
			}
			// Quaternion类型的value的关键帧
			class QuaternionKeyframe:CustomKeyframe<Quaternion>
			{
				public QuaternionKeyframe(float time,Quaternion value):base(time,value)
				{
				}
				// 线性插值
				public static QuaternionKeyframe Lerp(QuaternionKeyframe from, QuaternionKeyframe to,Vector2 t)
				{
					return new QuaternionKeyframe(
						Mathf.Lerp(from.time,to.time,t.x),
						Quaternion.Slerp(from.value,to.value,t.y)
					);
				}
				// 在线性插值上添加近似的贝塞尔曲线
				public static void AddBezierKeyframes(byte[] interpolation, int type,
					QuaternionKeyframe prev_keyframe,QuaternionKeyframe cur_keyframe, int interpolationQuality,
					ref QuaternionKeyframe[] keyframes,ref int index)
				{
					if(prev_keyframe==null || IsLinear(interpolation,type))
					{
						keyframes[index++]=cur_keyframe;
					}
					else
					{
						Vector2 bezierHandleA=GetBezierHandle(interpolation,type,0);
						Vector2 bezierHandleB=GetBezierHandle(interpolation,type,1);
						int sampleCount = interpolationQuality;
						for(int j = 0; j < sampleCount; j++)
						{
							float t=(j+1)/(float)sampleCount;
							Vector2 sample = SampleBezier(bezierHandleA,bezierHandleB,t);
							keyframes[index++] = QuaternionKeyframe.Lerp(prev_keyframe,cur_keyframe,sample);
						}
					}
				}
				
			}
			
			//求移动用的线性插值tangent
			float GetLinearTangentForPosition(Keyframe from_keyframe,Keyframe to_keyframe)
			{
				return (to_keyframe.value-from_keyframe.value)/(to_keyframe.time-from_keyframe.time);
			}
			//-359～+359度的范围转换为等价的0～359度
			float Mod360(float angle)
			{
				//剩余的计算用加算代替
				return (angle<0)?(angle+360f):(angle);
			}
			//求旋转的线性插值tangent
			float GetLinearTangentForRotation(Keyframe from_keyframe,Keyframe to_keyframe)
			{
				float tv=Mod360(to_keyframe.value);
				float fv=Mod360(from_keyframe.value);
				float delta_value=Mod360(tv-fv);
				//超过180度的情况使用反旋转
				if(delta_value<180f)
				{ 
					return delta_value/(to_keyframe.time-from_keyframe.time);
				}
				else
				{
					return (delta_value-360f)/(to_keyframe.time-from_keyframe.time);
				}
			}
			//动画Edit上选择BothLinear时的值
			private const int TangentModeBothLinear=21;
			
			//计算Unity的Keyframe（旋转用）
			void ToKeyframesForRotation(QuaternionKeyframe[] custom_keys,ref Keyframe[] rx_keys,ref Keyframe[] ry_keys,ref Keyframe[] rz_keys)
			{
				rx_keys=new Keyframe[custom_keys.Length];
				ry_keys=new Keyframe[custom_keys.Length];
				rz_keys=new Keyframe[custom_keys.Length];
				for(int i = 0; i < custom_keys.Length; i++)
				{
					//取得欧拉角
					Vector3 eulerAngles=custom_keys[i].value.eulerAngles;
					rx_keys[i]=new Keyframe(custom_keys[i].time,eulerAngles.x);
					ry_keys[i]=new Keyframe(custom_keys[i].time,eulerAngles.y);
					rz_keys[i]=new Keyframe(custom_keys[i].time,eulerAngles.z);
					//（进行？）线性插值
					rx_keys[i].tangentMode=TangentModeBothLinear;
					ry_keys[i].tangentMode=TangentModeBothLinear;
					rz_keys[i].tangentMode=TangentModeBothLinear;
					if(i>0)
					{
						float tx=GetLinearTangentForRotation(rx_keys[i-1],rx_keys[i]);
						float ty=GetLinearTangentForRotation(ry_keys[i-1],ry_keys[i]);
						float tz=GetLinearTangentForRotation(rz_keys[i-1],rz_keys[i]);
						rx_keys[i-1].outTangent=tx;
						ry_keys[i-1].outTangent=ty;
						rz_keys[i-1].outTangent=tz;
						rx_keys[i].inTangent=tx;
						ry_keys[i].inTangent=ty;
						rz_keys[i].inTangent=tz;
					}
				}
				AddDummyKeyframe(ref rx_keys);
				AddDummyKeyframe(ref ry_keys);
				AddDummyKeyframe(ref rz_keys);
			}
			
			
			// 取出骨骼所包含的关键帧
			// 这里只有旋转
			void CreateKeysForRotation(MMD.VMD.VMDFormat format, AnimationClip clip, string current_bone, string bone_path, int interpolationQuality)
			{
				try 
				{
					List<MMD.VMD.VMDFormat.Motion> mlist = format.motion_list.motion[current_bone];
					int keyframeCount = GetKeyframeCount(mlist, 3, interpolationQuality);
					
					QuaternionKeyframe[] r_keys = new QuaternionKeyframe[keyframeCount];
					QuaternionKeyframe r_prev_key=null;
					int ir=0;
					for (int i = 0; i < mlist.Count; i++)
					{
						const float tick_time = 1.0f / 30.0f;
						float tick = mlist[i].flame_no * tick_time;
						
						Quaternion rotation=mlist[i].rotation;
						QuaternionKeyframe r_cur_key=new QuaternionKeyframe(tick,rotation);
						QuaternionKeyframe.AddBezierKeyframes(mlist[i].interpolation,3,r_prev_key,r_cur_key,interpolationQuality,ref r_keys,ref ir);
						r_prev_key=r_cur_key;
					}
					
					Keyframe[] rx_keys=null;
					Keyframe[] ry_keys=null;
					Keyframe[] rz_keys=null;
					ToKeyframesForRotation(r_keys, ref rx_keys, ref ry_keys, ref rz_keys);
					AnimationCurve curve_x = new AnimationCurve(rx_keys);
					AnimationCurve curve_y = new AnimationCurve(ry_keys);
					AnimationCurve curve_z = new AnimationCurve(rz_keys);
					// 在这里设定旋转欧拉角（插值为四元素）
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.x",curve_x);
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.y",curve_y);
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.z",curve_z);
				}
				catch (KeyNotFoundException)
				{
					//Debug.LogError("加载的骨骼不兼容:" + bone_path);
				}
			}
			//计算Unity的Keyframe（移动用）
			Keyframe[] ToKeyframesForLocation(FloatKeyframe[] custom_keys)
			{
				Keyframe[] keys=new Keyframe[custom_keys.Length];
				for(int i = 0; i < custom_keys.Length; i++)
				{
					keys[i]=new Keyframe(custom_keys[i].time,custom_keys[i].value);
					//（进行）线性插值
					keys[i].tangentMode=TangentModeBothLinear;
					if(i>0)
					{
						float t=GetLinearTangentForPosition(keys[i-1],keys[i]);
						keys[i-1].outTangent=t;
						keys[i].inTangent=t;
					}
				}
				AddDummyKeyframe(ref keys);
				return keys;
			}
			// 只提取移动
			void CreateKeysForLocation(MMD.VMD.VMDFormat format, AnimationClip clip, string current_bone, string bone_path, int interpolationQuality, GameObject current_obj = null)
			{
				try
				{
					Vector3 default_position = Vector3.zero;
					if(current_obj != null)
						default_position = current_obj.transform.localPosition;
					
					List<MMD.VMD.VMDFormat.Motion> mlist = format.motion_list.motion[current_bone];
					
					int keyframeCountX = GetKeyframeCount(mlist, 0, interpolationQuality);
					int keyframeCountY = GetKeyframeCount(mlist, 1, interpolationQuality); 
					int keyframeCountZ = GetKeyframeCount(mlist, 2, interpolationQuality);
					
					FloatKeyframe[] lx_keys = new FloatKeyframe[keyframeCountX];
					FloatKeyframe[] ly_keys = new FloatKeyframe[keyframeCountY];
					FloatKeyframe[] lz_keys = new FloatKeyframe[keyframeCountZ];
					
					FloatKeyframe lx_prev_key=null;
					FloatKeyframe ly_prev_key=null;
					FloatKeyframe lz_prev_key=null;
					int ix=0;
					int iy=0;
					int iz=0;
					for (int i = 0; i < mlist.Count; i++)
					{
						const float tick_time = 1.0f / 30.0f;
						
						float tick = mlist[i].flame_no * tick_time;
						
						FloatKeyframe lx_cur_key=new FloatKeyframe(tick,mlist[i].location.x * scale_ + default_position.x);
						FloatKeyframe ly_cur_key=new FloatKeyframe(tick,mlist[i].location.y * scale_ + default_position.y);
						FloatKeyframe lz_cur_key=new FloatKeyframe(tick,mlist[i].location.z * scale_ + default_position.z);
						
						// 在个个轴上分别添加插值
						FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,0,lx_prev_key,lx_cur_key,interpolationQuality,ref lx_keys,ref ix);
						FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,1,ly_prev_key,ly_cur_key,interpolationQuality,ref ly_keys,ref iy);
						FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,2,lz_prev_key,lz_cur_key,interpolationQuality,ref lz_keys,ref iz);
						
						lx_prev_key=lx_cur_key;
						ly_prev_key=ly_cur_key;
						lz_prev_key=lz_cur_key;
					}
					
					// 在旋转骨骼下不应该加入数据
					if (mlist.Count != 0)
					{
						AnimationCurve curve_x = new AnimationCurve(ToKeyframesForLocation(lx_keys));
						AnimationCurve curve_y = new AnimationCurve(ToKeyframesForLocation(ly_keys));
						AnimationCurve curve_z = new AnimationCurve(ToKeyframesForLocation(lz_keys));
 						AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.x",curve_x);
						AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.y",curve_y);
						AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.z",curve_z);
					}
				}
				catch (KeyNotFoundException)
				{
					//Debug.LogError("加载的骨骼不兼容:" + current_bone);
				}
			}

			void CreateKeysForSkin(MMD.VMD.VMDFormat format, AnimationClip clip)
			{
				const float tick_time = 1f / 30f;

					// 搜索所有的表情关键帧
					List<VMD.VMDFormat.SkinData> s;

				foreach (var skin in format.skin_list.skin)
				{
					s = skin.Value;
					Keyframe[] keyframe = new Keyframe[skin.Value.Count];

					// 注册关键帧
					for (int i = 0; i < skin.Value.Count; i++) 
					{
						keyframe[i] = new Keyframe(s[i].flame_no * tick_time, s[i].weight);
						//进行线性插值
						keyframe[i].tangentMode=TangentModeBothLinear;
 						if(i>0)
						{
							float t=GetLinearTangentForPosition(keyframe[i-1],keyframe[i]);
							keyframe[i-1].outTangent=t;
							keyframe[i].inTangent=t;
 						}
					}
					AddDummyKeyframe(ref keyframe);

					// Z移动上添加插值
					AnimationCurve curve = new AnimationCurve(keyframe);
					AnimationUtility.SetEditorCurve(clip,"Expression/" + skin.Key,typeof(Transform),"m_LocalPosition.z",curve);
				}
			}
			
			// 取得骨骼的路径
			string GetBonePath(Transform transform)
			{
				string buf;
				if (transform.parent == null)
					return transform.name;
				else 
					buf = GetBonePath(transform.parent);
				return buf + "/" + transform.name;
			}
			
			// 递归搜索骨骼的子
			void FullSearchBonePath(Transform transform, Dictionary<string, string> dic)
			{
				int count = transform.GetChildCount();
				for (int i = 0; i < count; i++)
				{
					Transform t = transform.GetChild(i);
					FullSearchBonePath(t, dic);
				}
				
				// object的名字相加在一起 所以去除
				string buf = "";
				string[] spl = GetBonePath(transform).Split('/');
				for (int i = 1; i < spl.Length-1; i++)
					buf += spl[i] + "/";
				buf += spl[spl.Length-1];

				try
				{
					dic.Add(transform.name, buf);
				}
				catch (ArgumentException arg)
				{
					Debug.Log(arg.Message);
					Debug.Log("An element with the same key already exists in the dictionary. -> " + transform.name);
				}

				// dic里添加所有的骨骼名,骨骼路径
			}
			
			// 已注册无用的曲线该怎么做
			void FullEntryBoneAnimation(MMD.VMD.VMDFormat format, AnimationClip clip, Dictionary<string, string> dic, Dictionary<string, GameObject> obj, int interpolationQuality)
			{
				foreach (KeyValuePair<string, string> p in dic)	// key是transform的名字, value是path
				{
					// 互相名称相同的 情况下查一下Rigidbody是否存在
					GameObject current_obj = null;
					if(obj.ContainsKey(p.Key)){
						current_obj = obj[p.Key];
						
						// Rigidbody存在的话忽视关键帧的注册
						var rigid = current_obj.GetComponent<Rigidbody>();
						if (rigid != null && !rigid.isKinematic)
						{
							continue;
						}
					}
					
					// 注册关键帧
					CreateKeysForLocation(format, clip, p.Key, p.Value, interpolationQuality, current_obj);
					CreateKeysForRotation(format, clip, p.Key, p.Value, interpolationQuality);
				}
			}

			// 通过递归取得所有的gameobject
			void GetGameObjects(Dictionary<string, GameObject> obj, GameObject assign_pmd)
			{
				for (int i = 0; i < assign_pmd.transform.childCount; i++)
				{
					var transf = assign_pmd.transform.GetChild(i);
					try
					{
						obj.Add(transf.name, transf.gameObject);
					}
					catch (ArgumentException arg)
					{
						Debug.Log(arg.Message);
						Debug.Log("An element with the same key already exists in the dictionary. -> " + transf.name);
					}

					if (transf == null) continue;		// stopper 让某种东西停止的物件
					GetGameObjects(obj, transf.gameObject);
				}
			}

			private float scale_ = 1.0f;
		}
	}
}
