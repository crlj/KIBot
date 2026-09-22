using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
namespace MultiMind;
public partial class MainWindow : Window {
  readonly HttpClient http = new() { Timeout = Timeout.InfiniteTimeSpan };
  public MainWindow(){ InitializeComponent(); }
  string BaseUrl => BackendBox.Text.Trim().TrimEnd('/') + "/";
  async void LoadModels_Click(object sender, RoutedEventArgs e){
    try { StatusText.Text="Lade Modelle..."; var json=await http.GetStringAsync(BaseUrl+"models"); var arr=JsonDocument.Parse(json).RootElement; ModelBox.Items.Clear(); foreach(var m in arr.EnumerateArray()) ModelBox.Items.Add(m.GetProperty("id").GetString()); if(ModelBox.Items.Count>0) ModelBox.SelectedIndex=0; StatusText.Text="Modelle geladen"; }
    catch(Exception ex){ StatusText.Text="Fehler: "+ex.Message; }
  }
  async void Send_Click(object sender, RoutedEventArgs e){
    var prompt=PromptBox.Text.Trim(); if(prompt.Length==0)return; SendButton.IsEnabled=false; AnswerText.Text=""; StatusText.Text="Verbinde...";
    try {
      var payload=new { messages=new[]{new {role="user",content=prompt}}, mode="free", preferred_model=ModelBox.SelectedItem?.ToString(), allow_paid_fallback=false, max_output_tokens=700 };
      using var req=new HttpRequestMessage(HttpMethod.Post,BaseUrl+"chat/stream"){Content=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json")};
      using var res=await http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead); res.EnsureSuccessStatusCode(); using var stream=await res.Content.ReadAsStreamAsync(); using var reader=new StreamReader(stream);
      while(await reader.ReadLineAsync() is string line){ if(!line.StartsWith("data: "))continue; using var doc=JsonDocument.Parse(line[6..]); var r=doc.RootElement; var type=r.GetProperty("type").GetString();
        if(type=="token" && r.TryGetProperty("content",out var c)) AnswerText.Text+=c.GetString();
        else if(type=="model" && r.TryGetProperty("model",out var m)) StatusText.Text="Aktiv: "+m.GetString();
        else if(type=="fallback") StatusText.Text="Automatischer Modellwechsel...";
        else if(type=="done") StatusText.Text="Fertig";
        else if(type=="error") StatusText.Text=r.GetProperty("message").GetString();
      }
    } catch(Exception ex){ StatusText.Text="Fehler: "+ex.Message; } finally { SendButton.IsEnabled=true; }
  }
}
