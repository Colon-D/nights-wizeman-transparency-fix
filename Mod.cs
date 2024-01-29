using Reloaded.Mod.Interfaces;
using nights.test.wizemantransparencyfix.Template;
using nights.test.wizemantransparencyfix.Configuration;
using Reloaded.Hooks.Definitions;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;
using Reloaded.Hooks.Definitions.X86;
using static Reloaded.Hooks.Definitions.X86.FunctionAttribute;
using Reloaded.Memory.Sources;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace nights.test.wizemantransparencyfix;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
	/// <summary>
	/// Provides access to the mod loader API.
	/// </summary>
	private readonly IModLoader _modLoader;

	/// <summary>
	/// Provides access to the Reloaded.Hooks API.
	/// </summary>
	/// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
	private readonly IReloadedHooks _hooks;

	/// <summary>
	/// Provides access to the Reloaded logger.
	/// </summary>
	private readonly ILogger _logger;

	/// <summary>
	/// Entry point into the mod, instance that created this class.
	/// </summary>
	private readonly IMod _owner;

	/// <summary>
	/// Provides access to this mod's configuration.
	/// </summary>
	private Config _configuration;

	/// <summary>
	/// The configuration of the currently executing mod.
	/// </summary>
	private readonly IModConfig _modConfig;

	public Mod(ModContext context) {
		_modLoader = context.ModLoader;
		_hooks = context.Hooks;
		_logger = context.Logger;
		_owner = context.Owner;
		_configuration = context.Configuration;
		_modConfig = context.ModConfig;

		unsafe {
			wz_bgRenderHook = _hooks.CreateHook<wz_bgRender>(Wz_bgRenderImpl, 0x4BDFC0).Activate();

			AddToRenderQueueHook = _hooks.CreateHook<AddToRenderQueue>(RenderAddToQueueImpl, 0x591930).Activate();

			RenderFromRenderQueueHook = _hooks.CreateHook<RenderFromRenderQueue>(RenderFromRenderQueueImpl, 0x58D5B0).Activate();

			WizeRenderHook = _hooks.CreateHook<WizeRender>(WizeRenderImpl, 0x4448f0).Activate();

			// todo: find Wizeman's platform, and force it to render to 0x2C
		}
	}

	[Function(CallingConventions.Stdcall)]
	public unsafe delegate void wz_bgRender(void* param_1);
	public IHook<wz_bgRender> wz_bgRenderHook;
	public unsafe void Wz_bgRenderImpl(void* param_1) {
		countBeforeReset = 2;
		// change queue Wizeman's inner background renders to
		Memory.Instance.SafeWrite(0x591E8F, (byte)0x2C);
		wz_bgRenderHook.OriginalFunction(param_1);
	}

	[Function(new[] { Register.ecx, Register.edx }, Register.eax, StackCleanup.Callee)]
	public unsafe delegate void AddToRenderQueue(int a1, int a2, int a3, void* a4, int texture_or_model_maybe, float alpha);
	public IHook<AddToRenderQueue> AddToRenderQueueHook;
	public static int countBeforeReset = 0;
	public unsafe void RenderAddToQueueImpl(int a1, int a2, int a3, void* a4, int texture_or_model_maybe, float alpha) {
		AddToRenderQueueHook.OriginalFunction(a1, a2, a3, a4, texture_or_model_maybe, alpha);
		// decrement counter before resetting render queues
		if (countBeforeReset > 0) {
			--countBeforeReset;
			if (countBeforeReset == 0) {
				Memory.Instance.SafeWrite(0x591D39, (byte)0x1C);
				Memory.Instance.SafeWrite(0x591DAB, (byte)0x2C);
				Memory.Instance.SafeWrite(0x591E21, (byte)0x1C);
				Memory.Instance.SafeWrite(0x591E8F, (byte)0x0C);
			}
		}
	}

	[Function(Register.esi, Register.eax, StackCleanup.Callee)]
	public unsafe delegate int RenderFromRenderQueue(void* a1, byte translucent);
	public IHook<RenderFromRenderQueue> RenderFromRenderQueueHook;
	public unsafe int RenderFromRenderQueueImpl(void* a1, byte translucent) {
		// render object regardless of whether it is opaque or translucent
		RenderFromRenderQueueHook.OriginalFunction(a1, 0x00); // opaque
		return RenderFromRenderQueueHook.OriginalFunction(a1, 0x01); // translucent
	}

	[Function(CallingConventions.MicrosoftThiscall)]
	public unsafe delegate void WizeRender(void* wizeman);
	public IHook<WizeRender> WizeRenderHook;
	public unsafe void WizeRenderImpl(void* wizeman) {
		// normally 0x1C when translucent else 0x2C.
		// this would be perfect if I could find the platform code and force Wizeman's platform to render to 0x2C.
		Memory.Instance.SafeWrite(0x591D39, (byte)0x0C);
		// will be reset by Wz_bg, since it is queued after Wizeman
		WizeRenderHook.OriginalFunction(wizeman);
	}

	#region Standard Overrides
	public override void ConfigurationUpdated(Config configuration)
	{
		// Apply settings from configuration.
		// ... your code here.
		_configuration = configuration;
		_logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
	}
	#endregion

	#region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	public Mod() { }
#pragma warning restore CS8618
	#endregion
}
