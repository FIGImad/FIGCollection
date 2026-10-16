const fs=require('fs');
const [input,output]=process.argv.slice(2);
const lines=fs.readFileSync(input,'utf8').trim().split(/\r?\n/), headers=lines.shift().split(',');
// Study report text fields are quoted strings with doubled quotes.
const result=lines.map(line=>{
 const cells=line.match(/"(?:[^"]|"")*"|[^,]+|(?<=,)(?=,|$)/g);
 const v=Object.fromEntries(headers.map((k,i)=>{
  const s=cells[i]??'';
  return [k,s.startsWith('"')?s.slice(1,-1).replace(/""/g,'"'):s===''?null:Number(s)];
 }));
 return JSON.stringify({price:{RawTime:v.RawTime,Open:v.Open,High:v.High,Low:v.Low,Close:v.Close},values:v});
});
fs.writeFileSync(output,result.join('\n'));
