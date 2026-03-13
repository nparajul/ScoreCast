using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace ScoreCast.Web.Components.Reusable;

public partial class ScannableTextField<T> : MudTextField<T>
{
    private RenderFragment BaseContent => base.BuildRenderTree;
    [Parameter] public Func<Task<bool>>? Handler { get; set; }
    [Parameter] public Func<Task>? OnSuccess { get; set; }
    [Parameter] public Func<Task>? OnError { get; set; }
    [Parameter] public bool SubmitDisabled { get; set; }
    [Parameter] public bool Lockable { get; set; }
    [Parameter] public bool Locked { get; set; }
    private bool PreventDefault { get; } = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Variant = Variant.Outlined;
        OnKeyDown = new EventCallback<KeyboardEventArgs>(this, WrapTab);
        KeyDownPreventDefault = PreventDefault;
        OnKeyUp = new EventCallback<KeyboardEventArgs>(this, WrapEnter);
        Class = $"{Class} scannable";
    }

    public new async Task ClearAsync()
    {
        await base.ClearAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await Js.InvokeVoidAsync("setHasHandler", Handler != null);
            await Js.InvokeVoidAsync("setOnKeyDown");
        }
    }

    private async Task WrapTab(KeyboardEventArgs e)
    {
        if (e.Key == "Tab" && !SubmitDisabled)
            await WrapHandler();
    }

    private async Task WrapEnter(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !SubmitDisabled)
            await WrapHandler();
    }

    private async Task WrapHandler()
    {
        if (Handler is not null)
        {
            var success = await Handler!();
            if (success && OnSuccess is not null)
                await OnSuccess();
            else if (!success && OnError is not null)
                await OnError();
        }
        else if (OnSuccess is not null)
            await OnSuccess();

        base.ForceRender(true); // StateHasChanged doesn't always update the text
    }
}
