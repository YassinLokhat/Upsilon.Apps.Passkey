using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;

namespace Upsilon.Apps.Passkey.GUI.MAUI.ViewModels;

internal partial class PasswordGeneratorViewModel : ObservableObject
{
    public static string Title => MainViewModel.AppTitle + " - Password Generator";

    [ObservableProperty] private bool _checkIfLeaked = true;
    [ObservableProperty] private int _passwordLength = 20;
    [ObservableProperty] private string _generatedPassword = string.Empty;
    [ObservableProperty] private bool _includeNumerics = true;
    [ObservableProperty] private bool _includeSpecialCharacters = true;
    [ObservableProperty] private bool _includeLowerCaseAlphabet = true;
    [ObservableProperty] private bool _includeUpperCaseAlphabet = true;
    [ObservableProperty] private string _alphabet = string.Empty;

    public PasswordGeneratorViewModel()
    {
        _updateAlphabet();
        GeneratePassword();
    }

    [RelayCommand]
    internal void GeneratePassword()
    {
        GeneratedPassword = string.Empty;

        Task.Run(() =>
        {
            string result;
            try
            {
                // Normal mode (Windows)
                result = MainViewModel.PasswordFactory.GeneratePassword(PasswordLength, Alphabet, CheckIfLeaked);
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException || ex.InnerException is PlatformNotSupportedException)
            {
                // Safe mode (Android)
                result = _generateLocalFallback();
            }
            catch (Exception)
            {
                result = "Error";
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                GeneratedPassword = result;
            });
        });
    }

    private string _generateLocalFallback()
    {
        int lengthFallback = PasswordLength > 0 ? PasswordLength : 20;
        string fallbackChars = !string.IsNullOrEmpty(Alphabet) ? Alphabet : "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

        StringBuilder fallbackSb = new();
        for (int i = 0; i < lengthFallback; i++)
        {
            fallbackSb.Append(fallbackChars[Random.Shared.Next(fallbackChars.Length)]);
        }
        return fallbackSb.ToString();
    }

    partial void OnPasswordLengthChanged(int value) => GeneratePassword();
    partial void OnCheckIfLeakedChanged(bool value) => _updateAlphabet();
    partial void OnIncludeNumericsChanged(bool value) => _updateAlphabet();
    partial void OnIncludeSpecialCharactersChanged(bool value) => _updateAlphabet();
    partial void OnIncludeLowerCaseAlphabetChanged(bool value) => _updateAlphabet();
    partial void OnIncludeUpperCaseAlphabetChanged(bool value) => _updateAlphabet();

    private void _updateAlphabet() => Alphabet = _buildAlphabet();

    private string _buildAlphabet()
    {
        try
        {
            StringBuilder sb = new();
            if (IncludeNumerics) sb.Append(MainViewModel.PasswordFactory.Numeric);
            if (IncludeUpperCaseAlphabet) sb.Append(MainViewModel.PasswordFactory.Alphabetic.ToUpper());
            if (IncludeLowerCaseAlphabet) sb.Append(MainViewModel.PasswordFactory.Alphabetic.ToLower());
            if (IncludeSpecialCharacters) sb.Append(MainViewModel.PasswordFactory.SpecialChars);
            return sb.ToString();
        }
        catch (Exception)
        {
            // Immediate fallback if PasswordFactory crashes when reading text properties
            StringBuilder sb = new();
            if (IncludeNumerics) sb.Append("0123456789");
            if (IncludeUpperCaseAlphabet) sb.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            if (IncludeLowerCaseAlphabet) sb.Append("abcdefghijklmnopqrstuvwxyz");
            if (IncludeSpecialCharacters) sb.Append("!@#$%^&*()_+-=[]{}|;':\",./<>?");
            return sb.ToString();
        }
    }
}