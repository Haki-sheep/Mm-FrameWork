namespace MieMieFrameWork.CharacterController
{
    using MieMieFrameWork.MMAnimation;
    using UnityEngine;

    public interface I_CharacterControlBase : IIOCContainer
    {
        public void Init(CharacterControlBase cc);
    }

    /// <summary>
    /// 管理了玩家的基础的移动 旋转控制 地面检测 重力应用 斜坡处理
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(CharacterDataModule))]

    public abstract class CharacterControlBase : MonoBehaviour
    {
        #region Unity核心组件
        private CharacterController characterController;
        public CharacterController CharacterController { get => characterController; set => characterController = value;}
        #endregion

        #region 自定义组件
        protected IocContainer CcContainer;
        public IocContainer CCContainer=>CcContainer;
        
        protected CharacterDataModule characterDataBase; //注意Data是挂载组件 分发给其他组件 因此可以可以get set
        public CharacterDataModule CharacterDataBase { get => characterDataBase; set => characterDataBase = value; }

        protected GroundCheckModule groundCheckModule = new();
        protected RotateModule rotateModule = new();
        protected MoveMentModule moveMentModule = new();

        protected Vector3 horizontalMove;
        protected Vector3 verticalMove;

        protected virtual void InitCharacterControlBase()
        {

            characterDataBase ??= this.GetComponent<CharacterDataModule>();
            characterController ??= this.GetComponent<CharacterController>();

            CcContainer.Init();
            //添加对象 并做初始化
            CcContainer.AddComp2Dict<GroundCheckModule>(groundCheckModule);
            CcContainer.AddComp2Dict<RotateModule>(rotateModule);
            CcContainer.AddComp2Dict<MoveMentModule>(moveMentModule);

            // if (characterController is not null) Debug.Log("characterController: " + characterController);
            // if(characterDataBase is not null) Debug.Log("characterDataBase: " + characterDataBase);

            groundCheckModule.Init(this);
            rotateModule.Init(this);
            moveMentModule.Init(this);
        }

        #endregion

        protected virtual void Start()
        {
            InitCharacterControlBase();
            ModuleHub.Instance.GetManager<MonoManager>().AddUpdateListener(UpdateHandle);
            ModuleHub.Instance.GetManager<MonoManager>().AddFixedUpdateListener(FixedUpdateHandle);
        }

        protected virtual void OnDestroy()
        {
            ModuleHub.Instance.GetManager<MonoManager>().RemoveUpdateListener(UpdateHandle);
            ModuleHub.Instance.GetManager<MonoManager>().RemoveFixedUpdateListener(FixedUpdateHandle);
        }

        protected virtual void UpdateHandle()
        {
            //应用旋转
            switch (characterDataBase.ControllerMode)
            {
                case CharacterDataModule.PlayerControllerMode.FirstPerson:

                    horizontalMove = moveMentModule.GetMoveVectorF() * characterDataBase.MoveSpeed * Time.deltaTime;
                    Debug.Log("F: ");
                    break;
                case CharacterDataModule.PlayerControllerMode.ThirdPerson:
                    //计算位移
                    horizontalMove = moveMentModule.GetMoveVectorP() * Time.deltaTime * characterDataBase.MoveSpeed;
                    //应用旋转
                    characterController.transform.rotation =
                    rotateModule.GetThirdRotateVector(
                        characterController.transform.rotation, moveMentModule.GetMoveVectorP());
                    Debug.Log("T: ");
                    break;
            }

            //计垂直向量
            verticalMove = groundCheckModule.GetVerticalVector();
            // 应用移动
            if (characterController.enabled)
            {
                characterController.Move(horizontalMove + verticalMove);
            }
            else
            {
                this.transform.position += verticalMove;
            }
        }

        protected virtual void FixedUpdateHandle()
        {
            //地面检测
            groundCheckModule.CheckGroundHandle(this.transform);

            //斜坡处理
            moveMentModule.SlopeHandle(characterDataBase.IsGrounded, this.transform);
        }


    }
}



